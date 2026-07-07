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
