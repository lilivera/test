# PeerScope SI計画書

- 文書版数: 1.1
- 更新日: 2026-06-18
- 対象リポジトリ: `lilivera/PeerScope`
- 反映元コミット: `929df22` / Improve watch source scheduling and auto detection
- 登録先: `lilivera/test`

## 改版履歴

| 版数 | 日付 | 内容 |
|---|---|---|
| 1.0 | 2026-06-16 | 初版作成。構築、試験、リリース、運用引継ぎ計画を作成。 |
| 1.1 | 2026-06-18 | バージョンアップ反映。auto収集、スケジュール方式拡張、毎分Scheduler、CSVフォルダ取込、運用監視項目を更新。 |

## 1. 計画概要

PeerScopeをXAMPP Apache配下で稼働させ、Laravel 12、PHP 8.2以上、MySQL、Bootstrap 5、Laravel Schedulerにより、同業他社Webサイトの新着情報収集・閲覧・既読管理を提供する。

本SI計画では、環境構築、DB作成、初期データ投入、外部サイト収集設定、Scheduler設定、PDF保存先設定、CSVフォルダ取込設定、試験、リリース、運用引継ぎまでを対象とする。

## 2. スコープ

### 2.1 対象

- Laravelアプリケーション配置
- `.env` 設定
- MySQL DB作成、マイグレーション、シード
- Viteビルド成果物作成
- Apache URL設定、`.htaccess` 確認
- OSタスクスケジューラによる毎分 `php artisan schedule:run`
- admin初期ユーザー確認
- 会社マスタ、収集先マスタ初期設定
- PDF保存先、CSV取込/処理済み/失敗フォルダ作成と権限確認
- 機能テスト、収集テスト、運用テスト

### 2.2 対象外

- 外部サイト側のHTML/RSS仕様変更対応の恒久保証
- メール通知機能
- Active Directory連携
- SQLite運用
- PDF全文検索

## 3. 体制・役割

| 役割 | 担当内容 |
|---|---|
| 開発担当 | アプリ配置、設定、DB、ビルド、バージョンアップ反映、テスト支援。 |
| 運用担当 | Scheduler稼働確認、収集ログ確認、CSV失敗ファイル確認、容量監視。 |
| 利用部門 | 収集対象会社・URLの妥当性確認、新着内容の受入確認。 |
| 管理者ユーザー | ユーザー、会社、収集先、収集ログの画面管理。 |

## 4. 環境構築計画

| 項目 | 設定・確認内容 |
|---|---|
| PHP | 8.2以上。必要拡張: dom, curl, libxml, mbstring, pdo_mysql, zip。 |
| Webサーバ | XAMPP Apache。`http://localhost/PeerScope` 前提。 |
| DB | MySQL。DB名 `peerscope`、文字コード `utf8mb4`。 |
| Node | `npm install`、`npm run build` を実行。 |
| Laravel | `composer install`、`php artisan key:generate`、`php artisan migrate --seed`。 |
| Scheduler | OSタスクから毎分 `php artisan schedule:run`。 |
| Storage | PDF保存先、CSV取込/処理済み/失敗フォルダにWeb/CLI双方の書込権限を付与。 |

## 5. 導入作業計画

| No | 作業 | コマンド/確認 | 完了条件 |
|---:|---|---|---|
| 1 | ソース配置 | XAMPP Apache配下へ配置 | ブラウザからPeerScopeへ到達可能 |
| 2 | 依存関係 | `composer install`, `npm install` | エラーなく完了 |
| 3 | 環境設定 | `.env.example` を `.env` へコピーしDB/URLを設定 | APP_URL/ASSET_URL/DB設定が一致 |
| 4 | APP_KEY | `php artisan key:generate` | APP_KEYが設定済み |
| 5 | DB作成 | `CREATE DATABASE peerscope ...` | DB接続可能 |
| 6 | マイグレーション | `php artisan migrate --seed` | 主要テーブル・初期データ作成済み |
| 7 | ビルド | `npm run build` | public/build生成済み |
| 8 | 権限確認 | storage, bootstrap/cache | Web/CLIから書込可能 |
| 9 | Scheduler | OSタスク登録 | 毎分 `schedule:run` 実行ログを確認 |
| 10 | 収集先設定 | 管理画面で会社・収集先登録 | test-sourceで候補取得可能 |

## 6. 試験計画

| 区分 | 観点 |
|---|---|
| 環境試験 | URL到達、APP_KEY、DB接続、マイグレーション、ビルド、storage権限。 |
| 認証試験 | login_idログイン、ログアウト、admin/user権限制御、403確認。 |
| 管理試験 | ユーザー、会社、収集先の登録/更新、CSV取込、フォルダ選択。 |
| 収集試験 | auto/rss/html/js-news-list/json-news-list、PDF保存、重複抑止。 |
| Scheduler試験 | interval/daily/weekly/monthlyの期限判定、withoutOverlapping確認。 |
| 障害試験 | 外部HTTP 404、セレクタ不一致、PDF取得失敗、CSV不備、DB接続失敗。 |

## 7. リリース・切戻し計画

### 7.1 リリース手順

1. リリース前バックアップを取得する。
2. ソースを配置する。
3. `.env` を確認する。
4. `composer install --no-dev`、`npm run build` を実行する。
5. `php artisan migrate --force` を実行する。
6. `php artisan config:clear`、`php artisan route:clear`、`php artisan view:clear` を実行する。
7. Schedulerタスクを有効化する。
8. ログイン、ダッシュボード、手動収集、収集ログを確認する。

### 7.2 切戻し手順

1. Schedulerタスクを停止する。
2. Webアプリをメンテナンス状態または旧資材へ戻す。
3. DBバックアップから必要に応じて復旧する。
4. storage内PDF/CSVをリリース前状態へ戻す。
5. 旧版でログインと一覧表示を確認する。
6. 切戻し理由と影響範囲を記録する。

## 8. 運用引継ぎ

| 項目 | 引継ぎ内容 |
|---|---|
| 定常監視 | collection_runs、collection_errors、Scheduler実行、storage容量。 |
| 日次確認 | 前日以降の収集成功/失敗、CSV失敗フォルダ、PDF保存先容量。 |
| 障害時 | エラー会社、収集先URL、HTTPステータス、セレクタ、外部サイト変更を確認。 |
| 利用者対応 | ログイン不可、権限不備、既読状態、PDF取得不可の一次切分け。 |
| 改造引継ぎ | 収集方式・スケジュール・DB変更時の影響範囲とテスト観点。 |

## 9. リスク管理

| リスク | 影響 | 対策 |
|---|---|---|
| 外部サイトHTML変更 | 収集失敗または件数減少 | collection_errors監視、test-source、CSSセレクタ修正。 |
| Scheduler停止 | 新着が収集されない | OSタスク実行履歴、collection_runs最新日時を監視。 |
| PDF容量増加 | ディスク枯渇 | storage容量監視、保存期間方針の策定。 |
| CSV誤投入 | ユーザー登録不備 | 全件検証後反映、失敗ファイル退避、事前検証。 |
| DBマイグレーション失敗 | 起動不可 | 事前バックアップ、検証環境リハーサル、切戻し手順。 |
| 権限設定漏れ | 管理機能の誤利用 | admin middleware、受入試験、ユーザー棚卸。 |

## 10. 成果物

- PeerScope_システム仕様書.docx / md
- PeerScope_SI計画書.docx / md
- PeerScope_テストチェックリスト.xlsx / md
- PeerScope_設計書一式.zip
- `lilivera/test` 登録用Markdown一式

## 11. バージョンアップ反映作業

| 作業 | 内容 |
|---|---|
| 差分確認 | README、routes、Controller、Model、Service、Scheduler定義を確認。 |
| 設計反映 | auto収集、スケジュール方式拡張、毎分Scheduler、CSV/PDF運用を設計書へ反映。 |
| 試験反映 | チェックリストにauto収集、daily/weekly/monthly、PDF保存、CSV失敗退避を追加。 |
| リポジトリ登録 | Markdown版を `lilivera/test` の `docs/PeerScope/` 配下へ登録する。 |
