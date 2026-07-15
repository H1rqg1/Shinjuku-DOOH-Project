# Web to Unity data map

Webで選択した値をAPI経由でUnityへ渡す際の対応表です。Unity側では `AvatarCatalog` の `id` を論理アドレスとして使用します。Unity Addressablesのアセットアドレスとは別物です。

## Coordinate

| Web選択 | Web ID | API `costume_id` | UnityカタログID |
|---|---:|---|---|
| なし | 0 | `null` | 登録なし（Prefab既定表示） |
| コーデ1 | 1 | `costume_fashion01` | `costume_fashion01` |
| コーデ2 | 2 | `costume_fashion02` | `costume_fashion02` |
| コーデ3 | 3 | `costume_fashion03` | `costume_fashion03` |
| コーデ4 | 4 | `null` | 未登録 |

## Avatar code

`avatar_code` は `衣装4桁 + 帽子2桁 + アクセサリー2桁` です。例: コーデ1、帽子2、リボン3は `00010203` です。

| 種類 | Web選択 | ID |
|---|---|---:|
| 帽子 | なし / 帽子1 / 帽子2 / 帽子3 | 0 / 1 / 2 / 3 |
| アクセサリー | なし / メガネ / ネックレス / リボン | 0 / 1 / 2 / 3 |

## Messages

選択できるのは最大3件です。Webは選択したIDを `selected_message_ids` として保存し、プロフィール取得APIは `message_ids` として返します。

| 種類 | 表示メッセージ | 送信ID / UnityカタログID |
|---|---|---|
| あいさつ | こんにちは！ | `local_greet_1` |
| あいさつ | こんばんは！ | `local_greet_2` |
| あいさつ | おはようございます！ | `local_greet_3` |
| あいさつ | お疲れさまです！ | `local_greet_4` |
| あいさつ | 今日もいい日ですね！ | `local_greet_5` |
| あいさつ | 良い一日を！ | `local_greet_6` |
| あいさつ | ごゆっくりどうぞ！ | `local_greet_7` |
| あいさつ | ようこそ！ | `local_greet_8` |
| 気分 | ごきげんです | `local_mood_1` |
| 気分 | 元気です！ | `local_mood_2` |
| 気分 | のんびりしています | `local_mood_3` |
| 気分 | わくわくしています | `local_mood_4` |
| 気分 | リラックスしています | `local_mood_5` |
| 気分 | 少し眠いです | `local_mood_6` |
| 気分 | 穏やかな気分です | `local_mood_7` |
| 気分 | 今日は調子がいいです | `local_mood_8` |
| 気分 | 気楽に過ごしています | `local_mood_9` |
| 気分 | 笑顔です | `local_mood_10` |
| 今の様子 | 移動中です | `local_status_1` |
| 今の様子 | 休憩中です | `local_status_2` |
| 今の様子 | 待ち時間です | `local_status_3` |
| 今の様子 | 音楽を聴いています | `local_status_4` |
| 今の様子 | 景色を楽しんでいます | `local_status_5` |
| 今の様子 | 読書中です | `local_status_6` |
| 今の様子 | お散歩中です | `local_status_7` |
| 今の様子 | カフェでゆっくりしています | `local_status_8` |
| 今の様子 | ひと休みしています | `local_status_9` |
| 今の様子 | ぼーっとしています | `local_status_10` |
| 性格 | マイペースです | `local_trait_1` |
| 性格 | のんびり派です | `local_trait_2` |
| 性格 | 静かな時間が好きです | `local_trait_3` |
| 性格 | 好奇心旺盛です | `local_trait_4` |
| 性格 | 前向きです | `local_trait_5` |
| 性格 | ゆったり過ごしています | `local_trait_6` |
| 性格 | 楽しいことが好きです | `local_trait_7` |
| 性格 | コツコツ派です | `local_trait_8` |
| 性格 | 新しいことが好きです | `local_trait_9` |
| 性格 | 聞き役が多いです | `local_trait_10` |

## API contract

```json
{
  "costume_id": "costume_fashion01",
  "message_ids": ["local_greet_1", "local_mood_2"]
}
```

Web保存時のフィールド名は `selected_message_ids`、Unity受信時のフィールド名は `message_ids` です。

- `POST /sync`: `selected_message_ids` を保存
- `GET /profiles/recent`: `profiles[].message_ids` としてUnityへ返却
- Unity接続先: `https://shinjuku-dooh-api.pages.dev/profiles/recent`

UnityはCloudflareのプロフィールを内部の遭遇データへ変換します。`display_name` を表示名、`last_seen_at` を更新識別、`costume_id` を衣装検索、`message_ids` をメッセージ検索に使用します。`avatar_code` も受信データとして保持します。
