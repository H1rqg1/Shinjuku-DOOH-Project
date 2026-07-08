# DOOH Encounter Server

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

BLE検知スクリプトは以下をログに出します。

- bleakのimport確認
- OSとPythonバージョン
- スキャン開始
- 検出件数
- 検出デバイスの名前、アドレス、RSSI
- FastAPIへのPOST件数
- 例外発生時の詳細

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
