using System;
using System.Threading;
using System.Windows.Forms;

namespace AutoUploadIAToVagtools
{
    internal static class Program
    {
        private static Mutex mutex = null;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            const string appName = "AutoUploadIAToVagTools"; // 这里可以设置一个唯一的名称  
            bool createdNew;
            // 尝试获取互斥体  
            mutex = new Mutex(true, appName, out createdNew);
            if (createdNew)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
                // 释放互斥体  
                mutex.ReleaseMutex();
            }
            else
            {
                // 如果已存在实例，提醒用户或执行其他操作  
                MessageBox.Show("AutoUploadIAToVagTools Already run", "Tips", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
