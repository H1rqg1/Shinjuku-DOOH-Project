# DOOH Encounter Server

## Current BLE detection behavior

`app.py` does not scan Bluetooth devices directly. BLE detection is done by
`ble_scanner.py`, which uses `BleakScanner.discover()` and sends detected
devices to FastAPI `POST /encounter`.

Current defaults:

- RSSI filter: `--rssi-threshold -90`
- Disable RSSI filtering: `--no-rssi-threshold`
- POST limit per scan: unlimited by default (`--max-posts-per-scan 0`)
- Same device cooldown: `--cooldown-seconds 60`
- `target_id`: advertised BLE name when available, otherwise `null`
- Device address and RSSI are saved separately as `device_address` and `rssi`

This means unnamed BLE devices are no longer shown as address text above the
avatar. Unity receives `target_id: null`, so the avatar label becomes `None_01`,
`None_02`, etc. The server still counts unnamed devices by `device_address`.

Recommended startup:

```powershell
cd C:\Users\zhibu\GitHub\Shinjuku-DOOH-Project\server
.\.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt
python .\ble_scanner.py --server-url http://127.0.0.1:8000 --my-id dooh_pc
```

If detections are still too few, widen the range:

```powershell
python .\ble_scanner.py --rssi-threshold -95 --max-posts-per-scan 0
```

If you want to check everything the adapter can see, disable RSSI filtering:

```powershell
python .\ble_scanner.py --once --dry-run --no-rssi-threshold --log-level DEBUG
```

If detections are too many, narrow the range:

```powershell
python .\ble_scanner.py --rssi-threshold -75 --max-posts-per-scan 20
```

If you intentionally want to use BLE addresses as Unity labels:

```powershell
python .\ble_scanner.py --target-id-source address
```

UnityのDOOH表示プロジェクトへ、すれ違いデータを渡すためのFastAPIサーバーです。

## セットアップ

```powershell
cd C:\Users\Owner\DOOH_K\server
py -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt
```

実行ポリシーで有効化できない場合は、同じPowerShellで以下を実行してから再度有効化します。

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\.venv\Scripts\Activate.ps1
```

## 同じPCで起動

```powershell
python -m uvicorn app:app --reload --host 127.0.0.1 --port 8000
```

確認URL:

```text
http://127.0.0.1:8000/
http://127.0.0.1:8000/encounters
```

## BLE検知とUnity Playの起動手順

以下はWindows PowerShellでの基本手順です。FastAPIサーバー用とBLE検知用でPowerShellを2つ開きます。

### 1. FastAPIサーバーを起動

PowerShell 1:

```powershell
cd C:\Users\zhibu\GitHub\Shinjuku-DOOH-Project\server
.\.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt
python -m uvicorn app:app --reload --host 127.0.0.1 --port 8000
```

起動確認:

```powershell
Invoke-RestMethod http://127.0.0.1:8000/
Invoke-RestMethod http://127.0.0.1:8000/encounters
```

### 2. BLE検知スクリプトを起動

PowerShell 2:

```powershell
cd C:\Users\zhibu\GitHub\Shinjuku-DOOH-Project\server
.\.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt
python .\ble_scanner.py --server-url http://127.0.0.1:8000 --my-id dooh_pc
```

1回だけ診断したい場合:

```powershell
python .\ble_scanner.py --once --dry-run --log-level DEBUG
```

周囲のBLE端末が多い場所では、対象を絞って起動します。

```powershell
# 名前に AYA を含む端末だけを見る
python .\ble_scanner.py --once --dry-run --name-contains AYA

# RSSI -70以上の近い端末だけをFastAPIへ送る
python .\ble_scanner.py --rssi-threshold -70 --max-posts-per-scan 5

# 名前を広告している端末だけをFastAPIへ送る
python .\ble_scanner.py --require-name --max-posts-per-scan 5
```

BLE検知スクリプトは以下をログに出します。

- bleakのimport確認
- OSとPythonバージョン
- スキャン開始
- 検出件数とフィルタ後の件数
- 検出デバイスの名前、アドレス、RSSI
- FastAPIへのPOST件数
- 例外発生時の詳細

主なオプション:

- `--rssi-threshold -70`: 電波が弱い端末を無視します。数値を大きくすると近い端末だけになります。
- `--name-contains TEXT`: 名前に `TEXT` を含む端末だけ対象にします。
- `--require-name`: 名前なし端末を無視します。
- `--address-prefix AA:BB`: アドレス先頭が一致する端末だけ対象にします。
- `--cooldown-seconds 60`: 同じアドレスを再POSTするまでの秒数です。
- `--max-posts-per-scan 10`: 1回のスキャンでPOSTする最大件数です。
- `--dry-run`: FastAPIへPOSTせず、検出ログだけ確認します。

### 3. UnityをPlay

FastAPIサーバーとBLE検知スクリプトを起動した状態で、Unity Editorで `Assets/Scenes/DOOHScene.unity` を開いてPlayします。

確認ポイント:

- BLE検知ログに `Detected BLE devices` が出る
- `http://127.0.0.1:8000/encounters` に検知データが増える
- UnityのDOOH画面にアバターや本日の検出数が反映される

## BLEが動かない場合の確認

### Python仮想環境とbleak

```powershell
cd C:\Users\zhibu\GitHub\Shinjuku-DOOH-Project\server
.\.venv\Scripts\Activate.ps1
python -m pip show bleak
python .\ble_scanner.py --once --dry-run --log-level DEBUG
```

`No module named bleak` が出る場合:

```powershell
python -m pip install -r requirements.txt
```

### Windows Bluetooth状態

Windowsの設定でBluetoothがONか確認します。

```powershell
Get-PnpDevice -Class Bluetooth
Get-Service bthserv
```

確認すること:

- Bluetoothアダプタが存在する
- デバイス状態が `OK` になっている
- Bluetooth Support Service (`bthserv`) が停止していない
- Windowsの設定でBluetoothがOFFになっていない
- 別アプリがBluetoothアダプタを占有していない

### よくあるエラー

- `No module named bleak`: 仮想環境が未有効、または `python -m pip install -r requirements.txt` が未実行です。
- `Bluetooth radio is not powered on`: Windows側でBluetoothがOFFです。設定画面でBluetoothをONにしてから再実行します。
- `BLE scan failed`: BluetoothがOFF、BLE非対応アダプタ、ドライバ異常、Windows側の権限制限が疑われます。
- `POST failed: cannot reach FastAPI server`: FastAPIサーバーが起動していない、または `--server-url` が違います。
- 検出件数が0: 周囲にBLE広告を出す端末がない、距離が遠い、端末側のBluetoothがOFFの可能性があります。
- 検出件数は多いが対象端末が分からない: `--require-name`、`--name-contains`、`--rssi-threshold` で絞り込みます。

## 別PCから接続できるように起動

```powershell
python -m uvicorn app:app --reload --host 0.0.0.0 --port 8000
```

サーバーPCで `ipconfig` を実行し、Unity側の `serverUrl` を以下の形式に変更します。

```text
http://サーバーPCのIPv4アドレス:8000
```

## POSTテスト

```powershell
Invoke-RestMethod `
  -Uri "http://127.0.0.1:8000/encounter" `
  -Method Post `
  -ContentType "application/json" `
  -Body '{"my_id":"user_001","target_id":"user_002","timestamp":"2026-07-07T15:00:00+09:00"}'
```

## API

- `GET /`: 起動確認
- `POST /encounter`: すれ違いデータ保存
- `GET /encounters`: Unity向けの `{ "encounters": [...] }` を返す
- `DELETE /encounters`: 保存済みデータをリセット

`server/data/encounters.json` は存在しない場合に自動作成されます。
