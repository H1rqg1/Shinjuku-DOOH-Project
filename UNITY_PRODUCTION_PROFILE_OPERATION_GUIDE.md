# Unity 本番プロフィール表示確認 作業指示書

## 目的

公開Webで保存したプロフィールがCloudflare本番APIへ登録され、Unityの
`DOOHScene` に正しい名前・衣装・一言で表示されることを確認する。

本手順のプロフィール表示確認では、ローカルFastAPI、Uvicorn、BLEスキャナーは
起動しない。UnityはCloudflare本番APIを直接読み取る。

## 使用するURL

- 公開Web: `https://shinjukuweb.h1rqg1-makes-site.workers.dev/`
- 本番API: `https://shinjuku-dooh-api.pages.dev`
- プロフィール確認: `https://shinjuku-dooh-api.pages.dev/profiles/recent`

## 1. Unityプロジェクトを最新化する

Unityを閉じた状態でPowerShellを開き、次を実行する。

```powershell
cd "C:\Users\zhibu\GitHub\Shinjuku-DOOH-Project"
git status
git pull origin main
git log -5 --oneline
```

直近の履歴に、少なくとも次が表示されることを確認する。

```text
462f181 Integrate production profiles and fix avatar labels
```

`git status` に自分で編集した未コミットファイルが表示された場合は、消去や
上書きをせず、作業を止めて担当者へ確認する。

## 2. 本番APIを事前確認する

同じPowerShellで次を実行する。

```powershell
$response = Invoke-RestMethod `
  -Uri "https://shinjuku-dooh-api.pages.dev/profiles/recent" `
  -Method Get

$response.profiles |
  Select-Object display_name, costume_id, message_ids, last_seen_at |
  Format-Table -AutoSize
```

プロフィールが1件以上表示されれば、Unityが取得するデータは存在している。
通信エラーになる場合はUnityを起動する前に、インターネット接続とAPI URLを確認する。

## 3. Unityを起動する

Unity HubからUnity `6000.3.11f1` でプロジェクトを開く。
PowerShellから直接起動する場合は次を実行する。

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe" `
  -projectPath "C:\Users\zhibu\GitHub\Shinjuku-DOOH-Project"
```

起動後、画面右下のコンパイル表示が消えるまで待ち、次のシーンを開く。

```text
Assets/Scenes/DOOHScene.unity
```

## 4. Inspector設定を確認する

Hierarchyの `APIManager` を選択し、次を確認する。

- `Server Config`: `DOOHServerConfig_Production`
- `Show Existing Data On Start`: ON
- `Avatar Stay Seconds`: `600`

Hierarchyの `CrowdAvatarManager` を選択し、次を確認する。

- `Api Manager`: シーン内の `APIManager`
- `Avatar Prefab`: `Avatar`
- `Character Root`: シーン内の `CharacterRoot`
- `Avatar Catalog`: `New Avatar Catalog`
- `User Avatar Stay Seconds`: `600`
- `CPU Avatar Stay Seconds`: `30`

値が異なる場合はPlayを開始せず、最新コミットを取得できているか確認する。

## 5. Unity単体で表示確認する

1. UnityのConsoleを開く。
2. Consoleの既存ログをClearする。
3. `DOOHScene` のPlayボタンを押す。
4. 最大5秒待つ。

Consoleに次の接続先が表示されることを確認する。

```text
[DOOH API] Environment: Production
[DOOH API] Endpoint: https://shinjuku-dooh-api.pages.dev/profiles/recent
```

Gameビューで次を確認する。

- 登録ユーザーの名前が表示される。
- 日本語の一言が四角記号にならず表示される。
- 名前は常時表示され、一言は吹き出しに1件ずつランダム表示される。
- 吹き出しの表示開始、表示時間、次回表示までの間隔が毎回変化する。
- 吹き出しは同時に最大2件まで表示され、長い一言は2行以内に収まる。
- `今日の新宿の人数` と `現在時刻` が日本語フォントで表示される。
- `costume_id` に対応した衣装が表示される。
- 登録ユーザーが表示されたらCPUアバターが消える。
- Consoleに `Request failed`、`Response parse failed`、未登録ID警告がない。

## 6. Webから新規プロフィールを送信する

UnityをPlayしたまま、公開Webをブラウザで開く。

1. 他と区別できるテスト用ニックネームを入力する。
2. コーディネートを1件選択する。
3. 一言を1から3件選択する。
4. プロフィールを保存する。

Web画面はAPI通信結果にかかわらず `保存完了` と表示するため、その表示だけで
送信成功とは判断しない。PowerShellで本番APIを再確認する。

```powershell
$response = Invoke-RestMethod `
  -Uri "https://shinjuku-dooh-api.pages.dev/profiles/recent" `
  -Method Get

$response.profiles |
  Select-Object display_name, costume_id, message_ids, last_seen_at |
  Format-Table -AutoSize
```

テスト用ニックネームが表示された後、Unityに最大5秒以内で同じユーザーが
表示されることを確認する。

## 7. 表示時間を確認する

- 登録ユーザー: 約600秒（10分）表示する。
- CPUアバター: 約30秒で入れ替わる。
- 登録ユーザーは約599秒後から1秒かけてフェードアウトする。

10分確認では、表示開始時刻を記録し、9分30秒時点でユーザーが残っていることと、
約10分後にフェードアウトすることを確認する。

## 8. トラブルシューティング

### 登録ユーザーが表示されない

1. `/profiles/recent` にプロフィールがあるか確認する。
2. `APIManager` が `DOOHServerConfig_Production` を参照しているか確認する。
3. ConsoleのHTTPステータスと接続先URLを確認する。
4. Playを停止してから再度Playし、初回取得をやり直す。

### 日本語が四角で表示される

1. Unityを終了して開き直す。
2. `Assets/Fonts/NotoSansJP-VariableFont_wght SDF.asset` が存在するか確認する。
3. `Avatar` Prefabと `DOOHStatusDisplay` のFont Assetに同フォントが設定されているか確認する。

### 一言だけ表示されない

Consoleの `Message id is not registered in AvatarCatalog` を確認する。
現在のカタログにはWebの38メッセージIDが登録済みであるため、警告が出る場合は
WebとUnityで新しいIDが追加されていないか確認する。

### ユーザーが10秒で消える

`CrowdAvatarManager` の `User Avatar Stay Seconds` が `600` であることを確認する。
古いシーンが開かれている場合は `DOOHScene` を開き直す。

## 9. 完了条件

- `/profiles/recent` にテストプロフィールが存在する。
- Unity ConsoleにProductionのURLが表示される。
- 日本語の名前と一言が文字化けせず表示される。
- 一言が吹き出しでランダムなタイミングに表示される。
- Webで選択した衣装と一言がUnity表示に一致する。
- 登録ユーザーが約10分表示される。
- CPUアバターだけが約30秒で入れ替わる。
- Consoleに通信・JSON解析・未登録IDのエラーがない。

確認記録として、API一覧、Unity Gameビュー、Unity Consoleの3点をスクリーンショットで
残す。
