from datetime import datetime, timedelta, timezone
from pathlib import Path
from threading import Lock
from typing import Any, Dict, List, Optional
import json
from zoneinfo import ZoneInfo

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel


BASE_DIR = Path(__file__).resolve().parent
DATA_DIR = BASE_DIR / "data"
DATA_PATH = DATA_DIR / "encounters.json"
try:
    JST = ZoneInfo("Asia/Tokyo")
except Exception:
    JST = timezone(timedelta(hours=9), name="Asia/Tokyo")

data_lock = Lock()

app = FastAPI(title="DOOH Encounter Server")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


class Encounter(BaseModel):
    my_id: str
    target_id: Optional[str] = None
    device_name: Optional[str] = None
    device_address: Optional[str] = None
    rssi: Optional[int] = None
    timestamp: Optional[str] = None


def normalize_optional_text(value: Any) -> Optional[str]:
    if not isinstance(value, str):
        return None

    normalized_value = value.strip()
    return normalized_value or None


def is_none_like_id(value: Any) -> bool:
    normalized_value = normalize_optional_text(value)
    if normalized_value is None:
        return True

    return normalized_value.lower() in {"none", "null"}


def ensure_data_file() -> None:
    DATA_DIR.mkdir(parents=True, exist_ok=True)
    if not DATA_PATH.exists():
        write_encounters([])


def read_encounters() -> List[Dict[str, Any]]:
    ensure_data_file()

    try:
        with DATA_PATH.open("r", encoding="utf-8") as file:
            data = json.load(file)
    except (json.JSONDecodeError, OSError):
        return []

    if isinstance(data, dict):
        encounters = data.get("encounters", [])
    else:
        encounters = data

    if not isinstance(encounters, list):
        return []

    return [item for item in encounters if isinstance(item, dict)]


def write_encounters(encounters: List[Dict[str, Any]]) -> None:
    DATA_DIR.mkdir(parents=True, exist_ok=True)
    with DATA_PATH.open("w", encoding="utf-8") as file:
        json.dump(encounters, file, ensure_ascii=False, indent=2)


def encounter_to_dict(encounter: Encounter) -> Dict[str, Any]:
    if hasattr(encounter, "model_dump"):
        payload = encounter.model_dump()
    else:
        payload = encounter.dict()

    if is_none_like_id(payload.get("target_id")):
        payload["target_id"] = None
    else:
        payload["target_id"] = normalize_optional_text(payload.get("target_id"))

    payload["device_name"] = normalize_optional_text(payload.get("device_name"))
    payload["device_address"] = normalize_optional_text(payload.get("device_address"))

    if not payload.get("timestamp"):
        payload["timestamp"] = datetime.now(JST).isoformat(timespec="seconds")

    return payload


def get_detection_identity(encounter: Dict[str, Any]) -> Optional[str]:
    target_id = encounter.get("target_id")
    if not is_none_like_id(target_id):
        return f"target:{normalize_optional_text(target_id)}"

    device_address = normalize_optional_text(encounter.get("device_address"))
    if device_address is not None:
        return f"address:{device_address.lower()}"

    timestamp = normalize_optional_text(encounter.get("timestamp"))
    my_id = normalize_optional_text(encounter.get("my_id"))
    if timestamp is not None or my_id is not None:
        return f"anonymous:{my_id or 'unknown'}:{timestamp or 'unknown'}"

    return None


def parse_timestamp_to_jst(value: Any) -> Optional[datetime]:
    if not isinstance(value, str) or not value.strip():
        return None

    normalized_value = value.strip()
    if normalized_value.endswith("Z"):
        normalized_value = f"{normalized_value[:-1]}+00:00"

    try:
        parsed = datetime.fromisoformat(normalized_value)
    except ValueError:
        return None

    if parsed.tzinfo is None:
        parsed = parsed.replace(tzinfo=JST)

    return parsed.astimezone(JST)


def build_today_stats(encounters: List[Dict[str, Any]]) -> Dict[str, Any]:
    now_jst = datetime.now(JST)
    today_jst = now_jst.date()
    detected_ids = set()
    daily_encounter_count = 0

    for encounter in encounters:
        timestamp_jst = parse_timestamp_to_jst(encounter.get("timestamp"))
        if timestamp_jst is None or timestamp_jst.date() != today_jst:
            continue

        daily_encounter_count += 1

        detected_id = get_detection_identity(encounter)
        if detected_id is not None:
            detected_ids.add(detected_id)

    daily_detected_count = len(detected_ids) if detected_ids else daily_encounter_count

    return {
        "date_jst": now_jst.strftime("%Y-%m-%d"),
        "time_jst": now_jst.strftime("%H:%M"),
        "daily_detected_count": daily_detected_count,
        "daily_encounter_count": daily_encounter_count,
    }


@app.get("/")
def root() -> Dict[str, Any]:
    return {
        "message": "DOOH Encounter Server is running",
        "endpoints": {
            "save": "POST /encounter",
            "list": "GET /encounters",
            "stats": "GET /stats",
            "reset": "DELETE /encounters",
        },
    }


@app.post("/encounter")
def save_encounter(encounter: Encounter) -> Dict[str, Any]:
    payload = encounter_to_dict(encounter)

    with data_lock:
        encounters = read_encounters()
        encounters.append(payload)
        write_encounters(encounters)

    return {
        "message": "saved",
        "count": len(encounters),
        "encounter": payload,
    }


@app.get("/encounters")
def list_encounters() -> Dict[str, Any]:
    with data_lock:
        encounters = read_encounters()

    return {"encounters": encounters}


@app.get("/stats")
def get_stats() -> Dict[str, Any]:
    with data_lock:
        encounters = read_encounters()

    return build_today_stats(encounters)


@app.delete("/encounters")
def reset_encounters() -> Dict[str, Any]:
    with data_lock:
        write_encounters([])

    return {
        "message": "reset",
        "encounters": [],
    }
