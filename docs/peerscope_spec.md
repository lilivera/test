# PeerScope 基本仕様書

## 1. 文書情報

| 項目 | 内容 |
|---|---|
| システム名 | PeerScope |
| 文書名 | 基本仕様書 |
| 対象 | Laravel + MySQL を用いた同業他社新着情報収集サイト |
| 作成日 | 2026-06-01 |
| 版数 | 0.1 |
| 作成目的 | 初期開発に必要な機能、画面、データ構造、収集処理の仕様を整理する |

---

## 2. システム概要

PeerScope は、同業他社の公式ホームページ等に掲載される新着情報を定期的に収集し、社内利用者がログイン後に一覧表示、詳細確認、検索できるようにするWebシステムである。

主な目的は以下のとおり。

- 同業他社のニュース、IR、商品情報、採用情報、重要なお知らせ等を一元的に確認する。
- 各社サイトを個別に巡回する手間を削減する。
- 新着情報の検知日時、掲載日、収集元URL、抜粋を記録する。
- キーワード、会社、期間、カテゴリ等で過去の収集情報を検索する。
- 収集処理の成功、失敗、エラー内容を確認できるようにする。

本システムは情報収集・閲覧を目的とする。取得元サイトの内容を完全複製することは目的としない。画面上では、原則としてタイトル、掲載日、抜粋、元URLを中心に表示する。

---

## 3. 前提条件

### 3.1 技術前提

| 区分 | 採用技術 |
|---|---|
| Webフレームワーク | Laravel |
| 言語 | PHP |
| DB | MySQL |
| 画面 | Blade、Bootstrap等 |
| 認証 | Laravel標準認証、または Laravel Breeze 相当 |
| 定時実行 | Laravel Scheduler |
| 非同期処理 | Laravel Queue。ただし初期版では database ドライバを想定 |
| 検索 | MySQL FULLTEXT または LIKE検索 |

### 3.2 運用前提

- 利用者はログイン済みの社内ユーザーを想定する。
- 収集対象は管理者が登録する。
- 収集処理はサーバ上で定時実行される。
- 初期版では、RSSまたは通常HTMLで取得可能なページを主対象とする。
- JavaScript実行後にしか表示されないサイトは初期対象外とする。
- 取得元サイトへの過度なアクセスを避けるため、収集間隔を管理する。

---

## 4. 利用者区分

| 区分 | 権限 |
|---|---|
| 管理者 | ユーザー管理、会社管理、収集先管理、収集ログ確認、収集テスト、収集情報閲覧、検索 |
| 一般ユーザー | 新着一覧、詳細ビュワー、検索、既読管理 |

初期版では、ユーザー登録画面は公開しない。管理者がユーザーを登録する方式とする。

---

## 5. 機能一覧

| 機能ID | 機能名 | 概要 | 初期版対象 |
|---|---|---|---|
| F-001 | ログイン | ユーザー認証を行う | 対象 |
| F-002 | ログアウト | ログイン状態を解除する | 対象 |
| F-003 | ダッシュボード | 新着件数、未読件数、収集エラーを表示する | 対象 |
| F-004 | 新着一覧 | 収集した情報を一覧表示する | 対象 |
| F-005 | 詳細ビュワー | 収集情報の詳細を表示する | 対象 |
| F-006 | 検索 | キーワード、会社、期間等で検索する | 対象 |
| F-007 | 会社管理 | 監視対象会社を登録、編集、無効化する | 対象 |
| F-008 | 収集先管理 | RSS、HTML等の収集対象URLを登録する | 対象 |
| F-009 | 収集処理 | 登録された収集先から情報を取得する | 対象 |
| F-010 | 収集ログ | 収集結果、エラー内容を確認する | 対象 |
| F-011 | 既読管理 | ユーザー単位で既読、未読を管理する | 対象 |
| F-012 | 通知 | 新着や重要キーワードを通知する | 将来対応 |
| F-013 | CSV出力 | 検索結果をCSV出力する | 将来対応 |

---

## 6. 画面仕様

## 6.1 ログイン画面

### 目的

システム利用者を認証する。

### 入力項目

| 項目 | 必須 | 備考 |
|---|---|---|
| メールアドレス | 必須 | users.email |
| パスワード | 必須 | users.password |

### 処理

1. メールアドレス、パスワードを入力する。
2. 認証に成功した場合、ダッシュボードへ遷移する。
3. 認証に失敗した場合、エラーメッセージを表示する。

---

## 6.2 ダッシュボード画面

### 目的

収集状況の概要を確認する。

### 表示項目

| 項目 | 内容 |
|---|---|
| 本日の新着件数 | detected_at が当日の件数 |
| 未読件数 | ログインユーザーが未読の件数 |
| 直近収集日時 | collection_runs.finished_at の最新値 |
| 収集エラー件数 | 直近24時間のエラー件数 |
| 会社別新着件数 | 当日または直近7日間の会社別件数 |
| 最近の新着一覧 | 最新10件程度 |

### 操作

- 新着一覧へ遷移する。
- 収集エラー一覧へ遷移する。
- 各新着情報の詳細へ遷移する。

---

## 6.3 新着一覧画面

### 目的

収集済みの新着情報を一覧で確認する。

### 検索条件

| 項目 | 内容 |
|---|---|
| キーワード | タイトル、要約、本文テキストを対象に検索 |
| 会社 | company_id で絞り込み |
| カテゴリ | category で絞り込み |
| 掲載日From/To | published_at で絞り込み |
| 検知日From/To | detected_at で絞り込み |
| 既読状態 | 全て、未読のみ、既読のみ |

### 一覧表示項目

| 項目 | 内容 |
|---|---|
| 既読状態 | 未読、既読 |
| 検知日時 | detected_at |
| 掲載日 | published_at |
| 会社名 | companies.name |
| タイトル | collected_items.title |
| カテゴリ | collected_items.category |
| 収集元 | watch_sources.source_name |
| 元URL | collected_items.url |

### 並び順

初期表示は以下の順とする。

1. detected_at 降順
2. published_at 降順
3. id 降順

### ページング

1ページ50件を標準とする。

---

## 6.4 詳細ビュワー画面

### 目的

選択した新着情報の詳細を確認する。

### 表示項目

| 項目 | 内容 |
|---|---|
| 会社名 | 収集元会社 |
| タイトル | 新着情報タイトル |
| 掲載日 | 元サイトに掲載されている日付 |
| 検知日時 | 本システムが初回検知した日時 |
| 収集元名 | RSS、ニュース、IR等 |
| 元URL | 元サイトへのリンク |
| 要約 | 抜粋または自動生成した短文 |
| 本文テキスト | 保存している場合のみ表示 |
| 既読状態 | ログインユーザーの既読状態 |

### 処理

- 詳細画面を表示した時点で、ログインユーザーの既読情報を登録する。
- 元URLは別タブで開く。
- 本文全文の表示は必須としない。初期版では抜粋中心とする。

---

## 6.5 会社管理画面

### 目的

監視対象会社を管理する。

### 入力項目

| 項目 | 必須 | 内容 |
|---|---|---|
| 会社名 | 必須 | 表示名 |
| 公式URL | 任意 | 会社の公式サイトURL |
| メモ | 任意 | 補足情報 |
| 有効区分 | 必須 | 有効、無効 |

### 処理

- 会社を新規登録できる。
- 会社情報を編集できる。
- 不要になった会社は削除ではなく無効化する。

---

## 6.6 収集先管理画面

### 目的

会社ごとの収集対象URLおよび取得ルールを管理する。

### 入力項目

| 項目 | 必須 | 内容 |
|---|---|---|
| 会社 | 必須 | companies.id |
| 収集元名 | 必須 | 例：ニュース、IR、重要なお知らせ |
| 収集URL | 必須 | RSSまたはHTMLページURL |
| 収集方式 | 必須 | rss、html |
| 一覧行セレクタ | HTML時必須 | 各記事行を取得するCSSセレクタ |
| タイトルセレクタ | HTML時必須 | タイトル取得用CSSセレクタ |
| URLセレクタ | HTML時必須 | リンク取得用CSSセレクタ |
| 日付セレクタ | 任意 | 掲載日取得用CSSセレクタ |
| 本文セレクタ | 任意 | 詳細本文取得用CSSセレクタ |
| 収集間隔 | 必須 | 分単位 |
| 有効区分 | 必須 | 有効、無効 |

### 収集テスト

管理者は、登録した収集先に対してテスト取得を実行できる。

テスト結果には以下を表示する。

- HTTPステータス
- 取得件数
- 先頭数件のタイトル、URL、掲載日
- 解析エラー内容

---

## 6.7 収集ログ画面

### 目的

定時収集処理の実行結果を確認する。

### 表示項目

| 項目 | 内容 |
|---|---|
| 実行開始日時 | started_at |
| 実行終了日時 | finished_at |
| ステータス | success、warning、failed |
| 対象収集先数 | target_count |
| 新規登録件数 | created_count |
| 更新件数 | updated_count |
| エラー件数 | error_count |
| メッセージ | message |

---

## 7. 収集処理仕様

## 7.1 処理概要

Laravel Scheduler により、収集コマンドを定時実行する。

処理の流れは以下のとおり。

1. collection_runs に実行開始ログを登録する。
2. 有効な watch_sources を取得する。
3. 収集間隔、最終収集日時を確認し、実行対象を判定する。
4. 対象ごとにRSSまたはHTMLを取得する。
5. タイトル、URL、掲載日、本文抜粋を解析する。
6. URLハッシュ、内容ハッシュにより重複を判定する。
7. 新規情報を collected_items に登録する。
8. エラーが発生した場合、collection_errors に記録する。
9. collection_runs に実行結果を更新する。

---

## 7.2 スケジュール

初期設定では1時間に1回実行する。

```php
Schedule::command('peerscope:collect')
    ->hourly()
    ->withoutOverlapping(60);
```

サーバ側では以下のいずれかを利用する。

| OS | 実行方法 |
|---|---|
| Linux | cron で `php artisan schedule:run` を毎分実行 |
| Windows | タスクスケジューラで `php artisan schedule:run` を毎分実行 |

---

## 7.3 重複判定

重複判定は以下の順で行う。

1. URLを正規化する。
2. 正規化URLから SHA-256 ハッシュを生成する。
3. collected_items.url_hash に同一値が存在するか確認する。
4. 同一URLが存在しない場合、タイトル、掲載日、本文抜粋から content_hash を生成する。
5. content_hash が一致する場合は同一情報の可能性があるため、登録対象外または更新対象とする。

URL正規化では以下を行う。

- 相対URLを絶対URLに変換する。
- URL前後の空白を除去する。
- フラグメントを除去する。
- 必要に応じてトラッキング用クエリを除去する。

---

## 7.4 エラー処理

| エラー種別 | 処理 |
|---|---|
| HTTPエラー | collection_errors に記録し、次の収集先へ進む |
| タイムアウト | collection_errors に記録し、次の収集先へ進む |
| 解析エラー | 取得HTMLの保存は行わず、エラー内容を記録する |
| DB登録エラー | 対象データをロールバックし、エラーを記録する |
| 想定外エラー | collection_runs を failed または warning にする |

1件の収集先でエラーが発生しても、全体処理は可能な限り継続する。

---

## 7.5 アクセス制御

収集処理では以下を守る。

- 同一サイトへの短時間連続アクセスを避ける。
- 収集間隔は watch_sources ごとに管理する。
- タイムアウト値を設定する。
- User-Agent を明示する。
- 取得元サイトの利用規約、robots.txt、公開方針に配慮する。

---

## 8. データベース設計

## 8.1 companies

監視対象会社を管理する。

| カラム | 型 | NULL | 内容 |
|---|---|---|---|
| id | bigint unsigned | NO | 主キー |
| name | varchar(255) | NO | 会社名 |
| official_url | varchar(2048) | YES | 公式URL |
| memo | text | YES | 備考 |
| is_active | boolean | NO | 有効区分 |
| created_at | timestamp | YES | 作成日時 |
| updated_at | timestamp | YES | 更新日時 |

---

## 8.2 watch_sources

収集先を管理する。

| カラム | 型 | NULL | 内容 |
|---|---|---|---|
| id | bigint unsigned | NO | 主キー |
| company_id | bigint unsigned | NO | companies.id |
| source_name | varchar(255) | NO | 収集元名 |
| source_url | varchar(2048) | NO | 収集URL |
| source_type | varchar(20) | NO | rss、html |
| list_selector | varchar(1024) | YES | 一覧行セレクタ |
| title_selector | varchar(1024) | YES | タイトルセレクタ |
| url_selector | varchar(1024) | YES | URLセレクタ |
| date_selector | varchar(1024) | YES | 日付セレクタ |
| body_selector | varchar(1024) | YES | 本文セレクタ |
| crawl_interval_minutes | int | NO | 収集間隔 |
| last_crawled_at | datetime | YES | 最終収集日時 |
| is_active | boolean | NO | 有効区分 |
| created_at | timestamp | YES | 作成日時 |
| updated_at | timestamp | YES | 更新日時 |

---

## 8.3 collected_items

収集した新着情報を管理する。

| カラム | 型 | NULL | 内容 |
|---|---|---|---|
| id | bigint unsigned | NO | 主キー |
| company_id | bigint unsigned | NO | companies.id |
| watch_source_id | bigint unsigned | NO | watch_sources.id |
| title | varchar(1000) | NO | タイトル |
| url | varchar(2048) | NO | 元URL |
| url_hash | char(64) | NO | URLハッシュ |
| content_hash | char(64) | YES | 内容ハッシュ |
| published_at | datetime | YES | 掲載日 |
| detected_at | datetime | NO | 初回検知日時 |
| summary | text | YES | 要約、抜粋 |
| body_text | longtext | YES | 検索用本文テキスト |
| category | varchar(100) | YES | カテゴリ |
| created_at | timestamp | YES | 作成日時 |
| updated_at | timestamp | YES | 更新日時 |

### インデックス

| インデックス | カラム | 内容 |
|---|---|---|
| unique_url_hash | url_hash | URL重複防止 |
| idx_company_detected | company_id, detected_at | 会社別一覧 |
| idx_published_at | published_at | 掲載日検索 |
| fulltext_items | title, summary, body_text | キーワード検索 |

---

## 8.4 collection_runs

収集処理の実行単位を管理する。

| カラム | 型 | NULL | 内容 |
|---|---|---|---|
| id | bigint unsigned | NO | 主キー |
| started_at | datetime | NO | 開始日時 |
| finished_at | datetime | YES | 終了日時 |
| status | varchar(20) | NO | running、success、warning、failed |
| target_count | int | NO | 対象件数 |
| created_count | int | NO | 新規件数 |
| updated_count | int | NO | 更新件数 |
| error_count | int | NO | エラー件数 |
| message | text | YES | メッセージ |
| created_at | timestamp | YES | 作成日時 |
| updated_at | timestamp | YES | 更新日時 |

---

## 8.5 collection_errors

収集エラーを管理する。

| カラム | 型 | NULL | 内容 |
|---|---|---|---|
| id | bigint unsigned | NO | 主キー |
| collection_run_id | bigint unsigned | NO | collection_runs.id |
| watch_source_id | bigint unsigned | YES | watch_sources.id |
| error_type | varchar(255) | NO | エラー種別 |
| error_message | text | NO | エラー内容 |
| occurred_at | datetime | NO | 発生日時 |
| created_at | timestamp | YES | 作成日時 |
| updated_at | timestamp | YES | 更新日時 |

---

## 8.6 item_reads

ユーザー別の既読状態を管理する。

| カラム | 型 | NULL | 内容 |
|---|---|---|---|
| id | bigint unsigned | NO | 主キー |
| user_id | bigint unsigned | NO | users.id |
| collected_item_id | bigint unsigned | NO | collected_items.id |
| read_at | datetime | NO | 既読日時 |
| created_at | timestamp | YES | 作成日時 |
| updated_at | timestamp | YES | 更新日時 |

### 制約

- user_id と collected_item_id の組み合わせは一意とする。

---

## 9. ルーティング案

| メソッド | URL | 処理 |
|---|---|---|
| GET | /login | ログイン画面 |
| POST | /login | ログイン処理 |
| POST | /logout | ログアウト処理 |
| GET | /dashboard | ダッシュボード |
| GET | /items | 新着一覧、検索 |
| GET | /items/{id} | 詳細ビュワー |
| POST | /items/{id}/read | 既読登録 |
| GET | /companies | 会社一覧 |
| GET | /companies/create | 会社登録画面 |
| POST | /companies | 会社登録 |
| GET | /companies/{id}/edit | 会社編集画面 |
| PUT | /companies/{id} | 会社更新 |
| GET | /watch-sources | 収集先一覧 |
| GET | /watch-sources/create | 収集先登録画面 |
| POST | /watch-sources | 収集先登録 |
| GET | /watch-sources/{id}/edit | 収集先編集画面 |
| PUT | /watch-sources/{id} | 収集先更新 |
| POST | /watch-sources/{id}/test | 収集テスト |
| GET | /collection-runs | 収集ログ一覧 |
| GET | /collection-runs/{id} | 収集ログ詳細 |

管理系URLは管理者のみアクセス可能とする。

---

## 10. Artisanコマンド案

| コマンド | 内容 |
|---|---|
| peerscope:collect | 有効な収集先を対象に収集する |
| peerscope:collect-source {id} | 指定した収集先のみ収集する |
| peerscope:test-source {id} | 指定した収集先の取得テストを行う |

---

## 11. バリデーション仕様

## 11.1 会社登録

| 項目 | ルール |
|---|---|
| name | 必須、255文字以内 |
| official_url | URL形式、2048文字以内、任意 |
| memo | 任意 |
| is_active | boolean |

## 11.2 収集先登録

| 項目 | ルール |
|---|---|
| company_id | 必須、companiesに存在 |
| source_name | 必須、255文字以内 |
| source_url | 必須、URL形式、2048文字以内 |
| source_type | 必須、rss/html のいずれか |
| list_selector | source_type が html の場合は原則必須 |
| title_selector | source_type が html の場合は原則必須 |
| url_selector | source_type が html の場合は原則必須 |
| crawl_interval_minutes | 必須、1以上の整数 |
| is_active | boolean |

---

## 12. セキュリティ仕様

- 未ログインユーザーはログイン画面以外へアクセスできない。
- 管理機能は管理者のみ利用可能とする。
- パスワードはLaravel標準のハッシュ方式で保存する。
- CSRF対策はLaravel標準機能を使用する。
- XSS対策として、画面表示時はHTMLエスケープを基本とする。
- 収集した本文を表示する場合、元HTMLをそのまま表示しない。
- URLリンクは別タブ表示とし、必要に応じて `rel="noopener noreferrer"` を付与する。
- 管理画面で登録されたCSSセレクタやURLはサーバ側で検証する。

---

## 13. 非機能要件

## 13.1 性能

| 項目 | 目安 |
|---|---|
| 新着一覧表示 | 通常条件で3秒以内 |
| 検索表示 | 通常条件で5秒以内 |
| 収集処理 | 収集先50件で10分以内を目安 |

## 13.2 可用性

- 収集先ごとのエラーは全体停止につなげない。
- 収集処理の重複起動を防止する。
- 定時実行が失敗しても、次回実行で継続できる設計とする。

## 13.3 保守性

- 会社ごとの取得ルールはDBで管理し、プログラム修正を最小化する。
- 収集処理はRSS用、HTML用などのクラスに分離する。
- Controllerに収集ロジックを直接記述しない。
- 収集処理の結果はログとして確認できるようにする。

## 13.4 データ保持

初期案として、収集済みデータは削除しない。データ量が増加した場合、以下を検討する。

- 古い本文テキストのみ削除する。
- 一定期間以前のデータをアーカイブする。
- 検索対象期間を制限する。

---

## 14. ディレクトリ構成案

```text
app/
  Console/
    Commands/
      CollectPeerNews.php
      CollectPeerNewsSource.php
  Http/
    Controllers/
      DashboardController.php
      CollectedItemController.php
      CompanyController.php
      WatchSourceController.php
      CollectionRunController.php
  Models/
    Company.php
    WatchSource.php
    CollectedItem.php
    CollectionRun.php
    CollectionError.php
    ItemRead.php
  Services/
    Collection/
      NewsCollectorService.php
      RssCollector.php
      HtmlCollector.php
      UrlNormalizer.php
      ContentHashService.php
resources/
  views/
    dashboard.blade.php
    items/
    companies/
    watch_sources/
    collection_runs/
database/
  migrations/
```

---

## 15. 初期開発スコープ

初期開発では以下を実装対象とする。

1. ログイン、ログアウト
2. 管理者、一般ユーザーの権限区分
3. 会社管理
4. 収集先管理
5. RSS収集
6. HTML収集
7. 定時収集
8. 収集ログ
9. 新着一覧
10. 詳細ビュワー
11. キーワード検索
12. 既読、未読管理

以下は初期開発対象外とする。

- メール通知
- CSV出力
- AI要約
- JavaScript描画サイトの収集
- 添付ファイルの保存
- 外部検索エンジンの導入

---

## 16. 受入条件

| No | 条件 |
|---|---|
| 1 | ログインしていないユーザーが新着一覧へアクセスできないこと |
| 2 | 管理者が会社を登録、編集、無効化できること |
| 3 | 管理者が収集先を登録、編集、無効化できること |
| 4 | RSS形式の収集先から新着情報を登録できること |
| 5 | HTML形式の収集先からCSSセレクタに基づき新着情報を登録できること |
| 6 | 同一URLの情報が重複登録されないこと |
| 7 | 定時実行により収集処理が実行されること |
| 8 | 収集成功、収集失敗のログが確認できること |
| 9 | 新着一覧で会社、期間、キーワード検索ができること |
| 10 | 詳細ビュワーでタイトル、掲載日、検知日時、元URL、抜粋を確認できること |
| 11 | 詳細表示後、ログインユーザーの既読状態が登録されること |

---

## 17. 今後の拡張候補

- 重要キーワード登録と通知
- 会社別ウォッチリスト
- 新着情報への社内メモ登録
- タグ付け
- CSV出力
- 月次レポート作成
- Playwright等によるJavaScript描画サイト対応
- Laravel Scout、Meilisearch等による検索強化
- Slack、Teams、メール通知

---

## 18. 補足事項

収集対象サイトの構造変更により、HTML収集は突然失敗する可能性がある。そのため、収集先ごとのエラー状況を管理画面で確認できることを重視する。

また、本文の保存・表示範囲については、元サイトの利用条件や著作権上の扱いに注意する。初期版では、全文転載型ではなく、タイトル、掲載日、抜粋、元URLを中心とした参照型の画面構成とする。
