using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace iconConverter
{
    public partial class Form1 : Form
    {
        private readonly int[] IconSizes = new[] { 256, 128, 64, 32, 16, 8 };

        private readonly Dictionary<int, PictureBox> previews = new Dictionary<int, PictureBox>();
        private readonly Dictionary<int, CheckBox> checks = new Dictionary<int, CheckBox>();
        private readonly Dictionary<int, Bitmap> assigned = new Dictionary<int, Bitmap>();

        private Label lblStatus;
        private Button btnOutput;
        private Button btnClear;

        private TableLayoutPanel grid;   // プレビュー群
        private Panel scroll;            // スクロール領域（下揃えレイアウト制御）
        private Bitmap sourceBitmap;

        public Form1()
        {
            InitializeComponent();
            BuildUI();
            AllowDrop = true;
            DragEnter += OnDragEnter;
            DragDrop += OnDragDrop;
            FormClosed += delegate { DisposeAll(); };
        }

        private void BuildUI()
        {
            Text = "ICO 多サイズ同梱 出力ツール";
            Width = 900;
            Height = 600;
            StartPosition = FormStartPosition.CenterScreen;
            this.Padding = new Padding(8, 18, 8, 8);

            // ヘッダ
            var header = new Panel { Dock = DockStyle.Top, Height = 88 };
            var title = new Label { Text = "画像をドラッグ＆ドロップ。拡大はしない／縮小のみで各枠に割当。", Dock = DockStyle.Top, Height = 22 };
            lblStatus = new Label { Text = "未読込", Dock = DockStyle.Top, Height = 18 };

            var ops = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            btnOutput = new Button { Text = "出力", Width = 120, Height = 28 };
            btnClear = new Button { Text = "クリア", Width = 120, Height = 28 };
            btnOutput.Click += OnClickOutput;
            btnClear.Click += OnClickClear;
            ops.Controls.Add(btnOutput);
            ops.Controls.Add(btnClear);

            header.Controls.Add(ops);
            header.Controls.Add(lblStatus);
            header.Controls.Add(title);
            Controls.Add(header);

            // グリッド（Dockなし。位置は手動制御）
            grid = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 3,
                RowCount = 2,
                Padding = new Padding(8)
            };
            int[] order = new[] { 256, 128, 64, 32, 16, 8 };
            foreach (int s in order)
            {
                Control cell = BuildSizeCell(s);
                grid.Controls.Add(cell);
                assigned[s] = null;
            }

            // スクロールパネル（Resize時に下揃え配置）
            scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(0, 12, 0, 0) };
            scroll.Controls.Add(grid);
            scroll.Resize += delegate { LayoutGrid(); };
            Controls.Add(scroll);

            // フッタ
            var footer = new Label { Text = "チェックされたサイズのみを1つのICOに同梱（PNG-in-ICO）。", Dock = DockStyle.Bottom, Height = 20 };
            Controls.Add(footer);

            // 初期配置
            LayoutGrid();
        }

        // スクロール領域内で「下揃え」。横は中央寄せ。
        private void LayoutGrid()
        {
            if (grid == null || scroll == null) return;

            // 可視領域（スクロールバー分を考慮）
            Size client = scroll.DisplayRectangle.Size;

            int bottomPad = 12;
            int top = Math.Max(12, client.Height - grid.Height - bottomPad);
            int left = Math.Max(12, (client.Width - grid.Width) / 2);

            // AutoScroll有効時は Location ではなく AutoScrollPosition を使うと負の座標にされる。
            // ここは絶対位置でOK。スクロール原点はDisplayRectangle基準。
            grid.Location = new Point(left, top);
        }

        private Control BuildSizeCell(int size)
        {
            int pbEdge = size; // 等倍表示
            int panelW = Math.Max(260, pbEdge + 32);
            int panelH = pbEdge + 62;

            var panel = new Panel { Width = panelW, Height = panelH, Margin = new Padding(8) };
            var label = new Label { Text = size + " x " + size, Dock = DockStyle.Top, Height = 18 };

            var pb = new PictureBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                Width = pbEdge,
                Height = pbEdge,
                SizeMode = PictureBoxSizeMode.CenterImage,
                Left = (panel.Width - pbEdge) / 2,
                Top = 28,
                Tag = size,
                AllowDrop = true
            };
            pb.DragEnter += OnDragEnter;
            pb.DragDrop += OnDragDrop;
            previews[size] = pb;

            var cb = new CheckBox { Text = "このサイズを同梱", Dock = DockStyle.Bottom, Height = 24, Checked = true };
            checks[size] = cb;

            panel.Controls.Add(cb);
            panel.Controls.Add(pb);
            panel.Controls.Add(label);

            pb.Image = DrawPlaceholder(size);
            SetTip(pb, "空");
            return panel;
        }

        // D&D
        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0 && IsSupported(files[0])) { e.Effect = DragDropEffects.Copy; return; }
            }
            e.Effect = DragDropEffects.None;
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            try
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files == null || files.Length == 0) return;

                LoadSource(files[0]);
                MergeAssignmentsWithCurrent();   // 小さい画像は作れるサイズだけ上書き。大サイズは保持
                RefreshPreviews();
            }
            catch (Exception ex) { MessageBox.Show("読み込み失敗: " + ex.Message); }
        }

        private bool IsSupported(string path)
        {
            string ext = Path.GetExtension(path);
            if (ext == null) return false;
            ext = ext.ToLowerInvariant();
            return ext == ".png" || ext == ".bmp" || ext == ".gif" || ext == ".tif" || ext == ".tiff" || ext == ".jpg" || ext == ".jpeg";
        }

        private void LoadSource(string path)
        {
            if (sourceBitmap != null) { sourceBitmap.Dispose(); sourceBitmap = null; }

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var ms = new MemoryStream())
            {
                fs.CopyTo(ms); ms.Position = 0;
                using (var img = Image.FromStream(ms, true, true))
                {
                    sourceBitmap = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppArgb);
                    using (var g = Graphics.FromImage(sourceBitmap))
                    {
                        g.CompositingMode = CompositingMode.SourceCopy;
                        g.CompositingQuality = CompositingQuality.HighQuality;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.DrawImage(img, 0, 0, img.Width, img.Height);
                    }
                }
            }
            lblStatus.Text = "読込: " + Path.GetFileName(path) + " [" + sourceBitmap.Width + "x" + sourceBitmap.Height + "]";
        }

        // 既存割当を維持しつつマージ
        private void MergeAssignmentsWithCurrent()
        {
            if (sourceBitmap == null) return;

            int minSide = Math.Min(sourceBitmap.Width, sourceBitmap.Height);
            foreach (int s in IconSizes)
            {
                if (minSide >= s)
                {
                    using (var square = CenterCropToSquare(sourceBitmap))
                    {
                        var newBmp = ResizeBitmap(square, s, s);
                        if (assigned[s] != null) assigned[s].Dispose();
                        assigned[s] = newBmp;
                    }
                }
                // minSide < s の場合は既存を保持
            }
        }

        private void RefreshPreviews()
        {
            foreach (int s in IconSizes)
            {
                var pb = previews[s];
                if (pb.Image != null) { var old = pb.Image; pb.Image = null; old.Dispose(); }
                var bmp = assigned[s];
                if (bmp == null)
                {
                    pb.Image = DrawPlaceholder(s);
                    SetTip(pb, "空（拡大しないため未割当）");
                }
                else
                {
                    if (bmp.Width == pb.Width && bmp.Height == pb.Height)
                        pb.Image = (Bitmap)bmp.Clone();
                    else
                        pb.Image = ResizeBitmap(bmp, pb.Width, pb.Height);
                    SetTip(pb, "割当済み " + s + "x" + s);
                }
            }
            LayoutGrid(); // 画面更新後に再配置
        }

        private void OnClickOutput(object sender, EventArgs e)
        {
            try
            {
                var entries = IconSizes.Where(s => checks[s].Checked && assigned[s] != null)
                                       .Select(s => new IconEntry(s, assigned[s])).ToList();
                if (entries.Count == 0) { MessageBox.Show("出力対象なし。画像読込とサイズチェックを確認。"); return; }

                using (var sfd = new SaveFileDialog { Title = "ICOを書き出し", Filter = "Icon (*.ico)|*.ico", FileName = "icon.ico", OverwritePrompt = true })
                {
                    if (sfd.ShowDialog(this) != DialogResult.OK) return;
                    using (var fs = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write))
                    using (var bw = new BinaryWriter(fs))
                    {
                        WriteIcoPng(bw, entries);
                    }
                    MessageBox.Show("出力完了: " + sfd.FileName);
                }
            }
            catch (Exception ex) { MessageBox.Show("出力失敗: " + ex.Message); }
        }

        private void OnClickClear(object sender, EventArgs e)
        {
            if (sourceBitmap != null) { sourceBitmap.Dispose(); sourceBitmap = null; }
            lblStatus.Text = "未読込";
            foreach (int s in IconSizes)
            {
                if (assigned[s] != null) { assigned[s].Dispose(); assigned[s] = null; }
                var pb = previews[s];
                if (pb.Image != null) { var old = pb.Image; pb.Image = null; old.Dispose(); }
                pb.Image = DrawPlaceholder(s);
                SetTip(pb, "空");
            }
            LayoutGrid();
        }

        private void DisposeAll()
        {
            if (sourceBitmap != null) { sourceBitmap.Dispose(); sourceBitmap = null; }
            foreach (int s in IconSizes)
            {
                if (assigned[s] != null) { assigned[s].Dispose(); assigned[s] = null; }
                if (previews.ContainsKey(s) && previews[s].Image != null) previews[s].Image.Dispose();
            }
        }

        // 画像ユーティリティ
        private static Bitmap CenterCropToSquare(Bitmap src)
        {
            int size = Math.Min(src.Width, src.Height);
            int x = (src.Width - size) / 2;
            int y = (src.Height - size) / 2;
            var dst = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(dst))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(src, new Rectangle(0, 0, size, size), new Rectangle(x, y, size, size), GraphicsUnit.Pixel);
            }
            return dst;
        }

        private static Bitmap ResizeBitmap(Bitmap src, int w, int h)
        {
            var dst = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(dst))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(src, new Rectangle(0, 0, w, h), new Rectangle(0, 0, src.Width, src.Height), GraphicsUnit.Pixel);
            }
            return dst;
        }

        private static Bitmap DrawPlaceholder(int size)
        {
            var b = new Bitmap(128, 128, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(b))
            {
                g.Clear(Color.FromArgb(24, 24, 24));
                using (var pen = new Pen(Color.DimGray, 1)) { g.DrawRectangle(pen, 0, 0, 127, 127); }
                using (var fnt = new Font("Segoe UI", 10f, FontStyle.Regular))
                {
                    string txt = size + "x" + size;
                    SizeF sz = g.MeasureString(txt, fnt);
                    g.DrawString(txt, fnt, Brushes.Gray, (128 - sz.Width) / 2f, (128 - sz.Height) / 2f);
                }
            }
            return b;
        }

        private static void SetTip(Control c, string text)
        {
            ToolTip tt = c.Tag as ToolTip;
            if (tt == null) { tt = new ToolTip(); c.Tag = tt; }
            tt.SetToolTip(c, text);
        }

        // ICO書き込み（PNG-in-ICO）
        private static void WriteIcoPng(BinaryWriter bw, List<IconEntry> entries)
        {
            bw.Write((ushort)0);
            bw.Write((ushort)1);
            bw.Write((ushort)entries.Count);

            var pngDatas = entries.Select(delegate (IconEntry e) { return BitmapToPngBytes(e.Bitmap); }).ToList();

            int offset = 6 + 16 * entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                int size = entries[i].Size;
                byte w = (byte)(size == 256 ? 0 : Math.Min(size, 255));
                byte h = (byte)(size == 256 ? 0 : Math.Min(size, 255));
                byte[] data = pngDatas[i];

                bw.Write(w);
                bw.Write(h);
                bw.Write((byte)0);
                bw.Write((byte)0);
                bw.Write((ushort)0);
                bw.Write((ushort)32);
                bw.Write(data.Length);
                bw.Write(offset);

                offset += data.Length;
            }
            foreach (byte[] d in pngDatas) bw.Write(d);
        }

        private static byte[] BitmapToPngBytes(Bitmap bmp)
        {
            using (var ms = new MemoryStream())
            {
                var enc = ImageCodecInfo.GetImageDecoders().First(c => c.FormatID == ImageFormat.Png.Guid);
                using (var p = new EncoderParameters(1))
                {
                    p.Param[0] = new EncoderParameter(Encoder.ColorDepth, 32L);
                    bmp.Save(ms, enc, p);
                }
                return ms.ToArray();
            }
        }

        private sealed class IconEntry
        {
            public int Size { get; private set; }
            public Bitmap Bitmap { get; private set; }
            public IconEntry(int size, Bitmap bmp) { Size = size; Bitmap = bmp; }
        }
    }
}
