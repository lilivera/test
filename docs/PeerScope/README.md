# PeerScope 設計書一式

更新日: 2026-06-18  
対象: `lilivera/PeerScope`  
反映元: `929df22` / Improve watch source scheduling and auto detection

## 登録ファイル

- `PeerScope_システム仕様書.md`
  - 要件定義、基本設計、機能設計、DB/クラス設計、接続フロー、各プログラム処理フロー、障害対応、改造指針、テスト計画
- `PeerScope_SI計画書.md`
  - 構築、導入、試験、リリース、切戻し、運用引継ぎ、リスク管理
- `PeerScope_テストチェックリスト.md`
  - 環境、認証、管理、収集、Scheduler、CSV、障害、リリース観点

## バージョンアップ反映点

- `source_type=auto` によるURL自動判定収集を設計へ反映
- `interval/daily/weekly/monthly` のスケジュール方式を反映
- Laravel Schedulerを毎分起動し、各処理側で実行条件を判定する運用へ更新
- `WatchSource::isDue()` の期限判定、`NewsCollectorService` の自動判定、`HtmlCollector` の特殊形式/汎用HTML解析を処理フローへ反映
- PDF private保存、CSVフォルダ取込、収集ログ進捗更新の運用・テスト観点を更新

## 注意

DOCX/XLSX/ZIP版は別途成果物として作成済み。GitHubにはレビュー・履歴管理しやすいMarkdown版を登録する。
