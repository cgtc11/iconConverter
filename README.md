<img width="909" height="664" alt="B" src="https://github.com/user-attachments/assets/598c0775-8e32-4265-aaef-b088436eb563" /></br>
<!-- iconConverter README (HTML) -->
<h1>iconConverter — ICO 多サイズ同梱 出力ツール</h1>

<p>
Windows向けのアイコン生成ツール。ドラッグ＆ドロップした画像から
<strong>256/128/64/32/16/8 px</strong>の等倍プレビューを確認し、
チェックしたサイズだけを1つの <code>.ico</code> に同梱します（PNG-in-ICO）。
</p>

<hr />

<h2>主な機能</h2>
<ul>
  <li>画像をD&amp;Dで読込。拡大はせず<strong>縮小のみ</strong>で各サイズ枠へ自動割当</li>
  <li>等倍プレビュー（各枠は実ピクセル表示）</li>
  <li>チェックしたサイズのみをICOに同梱（PNG-in-ICO, 32bpp）</li>
  <li>「クリア」で全枠をリセット</li>
  <li>下揃え＆中央寄せレイアウトで大きいプレビューが見切れない</li>
</ul>

<h2>対応フォーマット</h2>
<p>
<strong>入力:</strong> PNG / BMP / GIF / TIFF / JPG / JPEG / TGA / ICO<br />
<strong>出力:</strong> ICO（各エントリはPNGストア）
</p>
<p>
<strong>注意:</strong> PSDは未対応（GDI+にデコーダ無し）。必要なら外部ライブラリや変換経由で対応。
</p>

<h2>割当ルール（重要）</h2>
<ul>
  <li>拡大は行わない。元画像の最小辺がサイズに満たない枠は「空」のまま</li>
  <li>正方形トリム（中央クロップ）→ 指定サイズへ高品質縮小</li>
  <li><strong>既に登録している画像がある場合、より大きいサイズの画像は削除しない</strong>
    （小さい画像を後から読み込んでも、大きい既存割当は保持）
  </li>
</ul>

<h2>使い方</h2>
<ol>
  <li>アプリを起動（初期サイズ: 900×600、中央表示）</li>
  <li>画像ファイルをウインドウまたは各プレビュー枠へドラッグ＆ドロップ</li>
  <li>必要なサイズにチェックを入れる（不要サイズはチェックを外す）</li>
  <li><strong>出力</strong>ボタン → 保存先を指定 → <code>icon.ico</code> を生成</li>
  <li>やり直す場合は<strong>クリア</strong>でリセット</li>
</ol>

<h2>画面の見方</h2>
<ul>
  <li>ヘッダ: 操作ボタン「出力」「クリア」、読込ステータス表示</li>
  <li>プレビュー: 256,128,64,32,16,8 の6枠。各枠は等倍。枠下のチェックで出力対象を選択</li>
  <li>フッタ: 「チェックされたサイズのみを1つのICOに同梱（PNG-in-ICO）。対応: PNG / BMP / GIF / TIFF / JPG / JPEG / TGA / ICO」</li>
</ul>

<h2>ICO仕様（出力の中身）</h2>
<ul>
  <li>各エントリはPNG画像（32bpp）を格納</li>
  <li>256pxの幅/高さはICOヘッダの仕様で<code>0</code>として記録</li>
</ul>

<h2>制限事項 / ヒント</h2>
<ul>
  <li>PSDは未対応。必要ならPNG変換後に読み込み</li>
  <li>TGAは24/32bitの非圧縮/RLEに対応。パレットTGAは対象外</li>
  <li>ICOを入力として読込む場合は、最大サイズフレームを優先的に取得</li>
  <li>プレビューは等倍表示。原寸確認が可能</li>
</ul>

<h2>トラブルシューティング</h2>
<ul>
  <li>読込に失敗: ファイル破損や未対応形式の可能性。別形式へ変換して再試行</li>
  <li>枠が「空」のまま: 元画像の最小辺がその枠サイズ未満（拡大は行わない仕様）</li>
  <li>大きい枠が消える: 仕様上、<em>小さい画像の読込では既存の大サイズ割当を削除しない</em>。必要に応じて「クリア」して再読込</li>
</ul>

<h2>動作環境</h2>
<ul>
  <li>Windows 10/11</li>
  <li>.NET Framework 4.7 以降（WinForms / System.Drawing）</li>
</ul>

<h2>ライセンス</h2>
<p>プロジェクトのライセンスに従う。</p>

<hr />

<details>
<summary>変更履歴（要約）</summary>
<ul>
  <li>入力形式に TGA / ICO を追加</li>
  <li>PSDは非対応の旨を明記</li>
  <li>フッタへ対応フォーマット表記を追加</li>
  <li>タイトル説明へ「既登録より大きい画像は削除しない」ルールを明記</li>
</ul>
</details>
