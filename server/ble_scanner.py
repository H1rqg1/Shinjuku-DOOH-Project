import argparse
import asyncio
import json
import logging
import platform
import sys
from datetime import datetime, timezone
from typing import Any, Dict, Iterable, List, Optional, Tuple
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

try:
    from bleak import BleakScanner
    from bleak.exc import BleakBluetoothNotAvailableError
except ImportError as exc:
    BleakScanner = None
    BleakBluetoothNotAvailableError = None
    BLEAK_IMPORT_ERROR = exc
else:
    BLEAK_IMPORT_ERROR = None


LOGGER = logging.getLogger("dooh_ble_scanner")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Scan BLE devices and send encounters to the DOOH FastAPI server.")
    parser.add_argument("--server-url", default="http://127.0.0.1:8000", help="FastAPI base URL.")
    parser.add_argument("--my-id", default=platform.node() or "windows_pc", help="ID reported as my_id.")
    parser.add_argument("--scan-seconds", type=float, default=5.0, help="Seconds per BLE scan.")
    parser.add_argument("--interval-seconds", type=float, default=5.0, help="Wait seconds between scans.")
    parser.add_argument("--rssi-threshold", type=int, default=None, help="Ignore devices weaker than this RSSI.")
    parser.add_argument("--name-contains", default=None, help="Only include devices whose name contains this text.")
    parser.add_argument("--address-prefix", default=None, help="Only include devices whose address starts with this text.")
    parser.add_argument("--require-name", action="store_true", help="Ignore devices with no advertised name.")
    parser.add_argument("--cooldown-seconds", type=float, default=60.0, help="Seconds before posting the same address again.")
    parser.add_argument("--max-posts-per-scan", type=int, default=10, help="Maximum encounters to POST per scan. Use 0 for no limit.")
    parser.add_argument("--max-log-devices", type=int, default=30, help="Maximum device rows to log per scan. Use 0 for no limit.")
    parser.add_argument("--once", action="store_true", help="Run one scan and exit.")
    parser.add_argument("--dry-run", action="store_true", help="Log devices without POSTing encounters.")
    parser.add_argument("--bleak-debug", action="store_true", help="Also show verbose logs from bleak internals.")
    parser.add_argument("--log-level", default="INFO", choices=["DEBUG", "INFO", "WARNING", "ERROR"])
    return parser.parse_args()


def normalize_server_url(server_url: str) -> str:
    return server_url.rstrip("/")


def post_encounter(server_url: str, my_id: str, target_id: str) -> None:
    payload = {
        "my_id": my_id,
        "target_id": target_id,
        "timestamp": datetime.now(timezone.utc).isoformat(timespec="seconds"),
    }
    data = json.dumps(payload).encode("utf-8")
    request = Request(
        f"{normalize_server_url(server_url)}/encounter",
        data=data,
        headers={"Content-Type": "application/json"},
        method="POST",
    )

    with urlopen(request, timeout=5) as response:
        body = response.read().decode("utf-8", errors="replace")
        LOGGER.debug("POST /encounter status=%s body=%s", response.status, body)


def device_rows(discovered: Any) -> List[Tuple[str, str, Any]]:
    rows: List[Tuple[str, str, Any]] = []

    if isinstance(discovered, dict):
        iterable: Iterable[Any] = discovered.values()
    else:
        iterable = discovered

    for item in iterable:
        if isinstance(item, tuple) and len(item) >= 2:
            device = item[0]
            advertisement = item[1]
            rssi = getattr(advertisement, "rssi", None)
        else:
            device = item
            rssi = getattr(device, "rssi", None)

        name = getattr(device, "name", None) or "(no name)"
        address = getattr(device, "address", None) or "(no address)"
        rows.append((name, address, rssi))

    return rows


def matches_filters(name: str, address: str, rssi: Any, args: argparse.Namespace) -> bool:
    if args.require_name and name == "(no name)":
        return False

    if args.name_contains and args.name_contains.lower() not in name.lower():
        return False

    if args.address_prefix and not address.lower().startswith(args.address_prefix.lower()):
        return False

    if args.rssi_threshold is not None and rssi is not None and int(rssi) < args.rssi_threshold:
        return False

    return True


def recently_posted(address: str, now_utc: datetime, posted_at: Dict[str, datetime], cooldown_seconds: float) -> bool:
    previous_posted_at = posted_at.get(address)
    if previous_posted_at is None:
        return False

    return (now_utc - previous_posted_at).total_seconds() < cooldown_seconds


async def scan_once(args: argparse.Namespace, posted_at: Dict[str, datetime]) -> int:
    if BleakScanner is None:
        LOGGER.error("bleak import failed: %s", BLEAK_IMPORT_ERROR)
        LOGGER.error("Install dependencies in the server venv: python -m pip install -r requirements.txt")
        return 1

    LOGGER.info("bleak import OK")
    LOGGER.info("OS: %s %s", platform.system(), platform.release())
    LOGGER.info("Python: %s", sys.version.split()[0])
    LOGGER.info("Scan start: %.1f seconds", args.scan_seconds)

    try:
        try:
            discovered = await BleakScanner.discover(timeout=args.scan_seconds, return_adv=True)
        except TypeError:
            discovered = await BleakScanner.discover(timeout=args.scan_seconds)
    except BleakBluetoothNotAvailableError as exc:
        LOGGER.error("BLE scan failed: %s", exc)
        LOGGER.error("Windows reports that Bluetooth is unavailable. Turn Bluetooth ON and check the BLE adapter/driver.")
        return 2
    except Exception:
        LOGGER.exception("BLE scan failed. Check that Bluetooth is ON, this PC has a BLE adapter, and Windows allows Bluetooth access.")
        return 2

    rows = device_rows(discovered)
    matched_rows = [row for row in rows if matches_filters(row[0], row[1], row[2], args)]
    LOGGER.info("Detected BLE devices: %d, matched filters: %d", len(rows), len(matched_rows))

    posted_count = 0
    logged_count = 0
    max_log_devices: Optional[int] = None if args.max_log_devices == 0 else max(0, args.max_log_devices)
    max_posts_per_scan: Optional[int] = None if args.max_posts_per_scan == 0 else max(0, args.max_posts_per_scan)

    for name, address, rssi in matched_rows:
        if max_log_devices is None or logged_count < max_log_devices:
            LOGGER.info("Device name=%s address=%s rssi=%s", name, address, rssi)
            logged_count += 1

        if address == "(no address)" or args.dry_run:
            continue

        now_utc = datetime.now(timezone.utc)
        if recently_posted(address, now_utc, posted_at, args.cooldown_seconds):
            LOGGER.debug("Skip recently posted device address=%s cooldown=%.1fs", address, args.cooldown_seconds)
            continue

        if max_posts_per_scan is not None and posted_count >= max_posts_per_scan:
            LOGGER.warning("Max posts per scan reached: %d", max_posts_per_scan)
            break

        try:
            post_encounter(args.server_url, args.my_id, address)
            posted_at[address] = now_utc
            posted_count += 1
        except HTTPError as exc:
            LOGGER.error("POST failed status=%s reason=%s", exc.code, exc.reason)
        except URLError as exc:
            LOGGER.error("POST failed: cannot reach FastAPI server at %s (%s)", args.server_url, exc.reason)
        except Exception:
            LOGGER.exception("POST failed unexpectedly")

    LOGGER.info("Posted encounters: %d", posted_count)
    return 0


async def main_async() -> int:
    args = parse_args()
    logging.basicConfig(
        level=getattr(logging, args.log_level),
        format="%(asctime)s %(levelname)s %(message)s",
    )
    if not args.bleak_debug:
        logging.getLogger("bleak").setLevel(logging.WARNING)

    posted_at: Dict[str, datetime] = {}
    while True:
        exit_code = await scan_once(args, posted_at)
        if args.once or exit_code != 0:
            return exit_code

        await asyncio.sleep(max(0.1, args.interval_seconds))


def main() -> int:
    try:
        return asyncio.run(main_async())
    except KeyboardInterrupt:
        LOGGER.info("BLE scanner stopped by user")
        return 0


if __name__ == "__main__":
    raise SystemExit(main())
