# PeerScope テストチェックリスト

- 文書版数: 1.1
- 更新日: 2026-06-18
- 反映元コミット: `929df22` / Improve watch source scheduling and auto detection

| No | 区分 | テスト項目 | 手順 | 期待結果 | 判定 |
|---:|---|---|---|---|---|
| 1 | 環境 | Laravel起動 | `/PeerScope`へアクセス | ログイン画面が表示される | 未 |
| 2 | 環境 | DB接続 | `php artisan migrate:status` | migration状態を取得できる | 未 |
| 3 | 環境 | Storage権限 | PDF保存先/CSV各フォルダへ書込 | Web/CLI双方で書込可能 | 未 |
| 4 | 認証 | adminログイン | login_id=adminでログイン | ダッシュボード表示、管理メニュー表示 | 未 |
| 5 | 認証 | 一般ユーザー制御 | userで管理URLへアクセス | 403または管理メニュー非表示 | 未 |
| 6 | 新着 | 一覧検索 | キーワード/会社/日付/既読条件を指定 | 条件に合う記事のみ表示 | 未 |
| 7 | 新着 | 詳細既読化 | 未読記事の詳細を開く | item_readsが作成され既読表示 | 未 |
| 8 | PDF | PDFダウンロード | PDF保存済み記事から取得 | 認証済み経路でPDF取得 | 未 |
| 9 | 会社 | 会社登録/更新 | name/URL/is_activeを更新 | 一覧に反映される | 未 |
| 10 | 収集先 | auto収集テスト | source_type=autoでtest-source | 候補記事が取得される | 未 |
| 11 | 収集先 | HTML詳細収集 | CSSセレクタを指定してtest | 指定箇所から候補抽出 | 未 |
| 12 | 収集 | RSS/Atom収集 | RSS URLを収集 | item/entryが共通形式で保存 | 未 |
| 13 | 収集 | JS一覧収集 | js-news-list設定 | JS配列から記事取得 | 未 |
| 14 | 収集 | JSON一覧収集 | json-news-list設定 | JSONデータから記事取得 | 未 |
| 15 | 収集 | 重複抑止 | 同じURLを再収集 | url_hashで重複登録されない | 未 |
| 16 | 収集 | 内容重複抑止 | URL違い同内容を収集 | content_hashで重複抑止 | 未 |
| 17 | Scheduler | interval | 短い間隔で設定 | 期限到来時だけ収集 | 未 |
| 18 | Scheduler | daily | schedule_timeを設定 | 当日指定時刻後に1回実行 | 未 |
| 19 | Scheduler | weekly | 曜日指定 | 指定曜日のみ実行 | 未 |
| 20 | Scheduler | monthly | 日付指定 | 指定日のみ実行 | 未 |
| 21 | CSV | 画面CSV取込 | 正常CSVをアップロード | 登録/削除件数が表示 | 未 |
| 22 | CSV | 不正CSV | 必須列不足CSVを投入 | DB更新せずエラー表示 | 未 |
| 23 | CSV | フォルダ取込成功 | inboxへCSV配置し実行 | processedへ移動 | 未 |
| 24 | CSV | フォルダ取込失敗 | 不正CSV配置 | failedへ移動し.error.txt作成 | 未 |
| 25 | 障害 | 外部HTTP失敗 | 404 URLを収集 | collection_errorsへ記録し他は継続 | 未 |
| 26 | 障害 | PDF取得失敗 | PDF取得不可URL | 記事収集は継続 | 未 |
| 27 | ログ | 実行中進捗 | 手動収集を起動 | collection_runs.messageが更新 | 未 |
| 28 | リリース | キャッシュクリア | config/route/view clear | 画面正常表示 | 未 |

## 重点回帰観点

### URL自動判定収集

- RSS/Atomを直接返すURLで候補が取得できること。
- HTML内のfeed linkを発見してRSS解析へ進めること。
- JS配列形式、JSON一覧形式、汎用HTMLリンク形式のいずれかで候補取得できること。
- 取得対象を見つけられない場合、収集ログまたはテスト画面で原因が確認できること。

### スケジュール方式

- `interval`: 最終収集日時 + 指定分数を過ぎたときだけ実行されること。
- `daily`: 指定時刻後に当日未実行の場合だけ実行されること。
- `weekly`: 指定曜日かつ指定時刻後だけ実行されること。
- `monthly`: 指定日かつ指定時刻後だけ実行されること。
- OSタスクスケジューラは毎分 `php artisan schedule:run` を実行すること。

### 運用障害

- `collection_runs.status` が `failed` になった場合、`collection_errors` から会社・収集先・エラー種別・内容が追えること。
- CSVフォルダ取込失敗時、対象CSVがfailedへ移動し、同名の `.error.txt` が保存されること。
- PDF取得失敗時、記事収集全体は継続し、後続記事の保存に影響しないこと。
