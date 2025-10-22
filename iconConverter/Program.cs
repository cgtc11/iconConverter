using System;
using System.Windows.Forms;

namespace iconConverter   // ← ここを iconConverter に
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());   // ← 全角やタイプミスがないか確認
        }
    }
}
