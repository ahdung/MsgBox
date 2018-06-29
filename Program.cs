using System;
using System.Windows.Forms;

namespace AhDung.WinForm
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FmTester());
            //Application.Run(new FmTestSound());
        }
    }
}
