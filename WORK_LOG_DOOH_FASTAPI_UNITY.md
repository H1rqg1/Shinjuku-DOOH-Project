# DOOH FastAPI Unity Integration Work Log

## 実装日

2026-07-07

## 目的

UnityのDOOH表示プロジェクトに、FastAPIで保存したすれ違いデータを定期取得し、Avatar Prefabとして画面へ表示する仕組みを組み込んだ。

## 変更内容

- FastAPIサーバーを `server/` 配下に追加した。
- `POST /encounter`、`GET /encounters`、`DELETE /encounters`、`GET /` を実装した。
- `server/data/encounters.json` をPC依存パスなしで自動生成・読み書きするようにした。
- Unityの `APIManager` を、サーバーURL・取得間隔・Prefab・表示時間・最大表示数・生成範囲をInspectorから調整できる形に更新した。
- Unityの `JsonUtility` で扱いやすい `{ "encounters": [...] }` 形式のレスポンスに対応した。
- `my_id + target_id + timestamp` をキーにして、同じすれ違いデータの重複表示を防ぐようにした。
- `AvatarView` を実装し、target ID表示、上下浮遊、フェードアウト、自動削除に対応した。
- `BillboardToCamera` を追加し、2Dアバターがカメラ方向を向くようにした。
- 既存の `CrowdAvatarManager` は互換用に残し、APIManager側のPrefab生成が有効な場合は二重生成を避けるようにした。

## Unityで手動設定が必要な項目

シーン内の `APIManager` に以下を設定する。

- `Server Url`: `http://127.0.0.1:8000`
- `Fetch Interval Seconds`: `1`
- `Show Existing Data On Start`: 必要に応じてON/OFF
- `Avatar Prefab`: `Assets/Prefabs/Avatar.prefab`
- `Character Root`: Hierarchy上の `CharacterRoot`
- `Avatar Stay Seconds`: `10`
- `Max Avatars On Screen`: `30`
- `Spawn Center`: 表示したい中心座標
- `Spawn Area Size`: `(12, 5, 4)`

既存シーンでは `CrowdAvatarManager` が `APIManager` のイベントを購読してPrefab生成する設定も残している。APIManagerに `Avatar Prefab` と `Character Root` を設定した場合は、APIManager側の生成が優先される。

## サーバー起動コマンド

```powershell
cd C:\Users\Owner\DOOH_K\server
py -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt
python -m uvicorn app:app --reload --host 127.0.0.1 --port 8000
```

別PCから接続する場合:

```powershell
python -m uvicorn app:app --reload --host 0.0.0.0 --port 8000
```

## POSTテスト

```powershell
Invoke-RestMethod `
  -Uri "http://127.0.0.1:8000/encounter" `
  -Method Post `
  -ContentType "application/json" `
  -Body '{"my_id":"user_001","target_id":"user_002","timestamp":"2026-07-07T15:00:00+09:00"}'
```

## Unityで確認すること

- FastAPIサーバー起動後、`http://127.0.0.1:8000/encounters` がブラウザで見えること。
- UnityのPlay中に、`CharacterRoot` 配下へ `Avatar_user_002` のようなGameObjectが生成されること。
- 同じencounterが繰り返し表示されないこと。
- `Avatar Stay Seconds` 経過後にAvatarがフェードアウトして削除されること。

## 注意点

- `server/data/encounters.json` は実行時に自動生成されるため、Git管理対象から外している。
- Unity側のPrefabやScene参照は、Unity Editorで一度開いて必要なInspector項目を確認すること。
- 別PC接続時はWindows Defender FirewallやWi-Fiの端末間通信制限を確認すること。
