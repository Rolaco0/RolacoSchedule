using System;
using System.Windows;
using System.Windows.Threading;

namespace RolacoSchedule
{
    public partial class NotificationWindow : Window
    {
        private DispatcherTimer autoCloseTimer;
        private MainWindow mainWindow;

        public NotificationWindow(MainWindow mainWindow = null)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;

            autoCloseTimer = new DispatcherTimer();
            autoCloseTimer.Interval = TimeSpan.FromSeconds(10);
            autoCloseTimer.Tick += AutoCloseTimer_Tick;
            autoCloseTimer.Start();
        }

        public void SetNotificationContent(string title, string taskInfo = null, string duration = null)
        {
            if (!string.IsNullOrEmpty(title))
            {
                txtSessionTitle.Text = title;
            }

            if (!string.IsNullOrEmpty(taskInfo))
            {
                txtTaskInfo.Text = taskInfo;
            }

            if (!string.IsNullOrEmpty(duration))
            {
                txtDuration.Text = duration;
            }
        }

        private void AutoCloseTimer_Tick(object sender, EventArgs e)
        {
            autoCloseTimer?.Stop();
            this.Close();
        }

        private void TakeBreak_Click(object sender, RoutedEventArgs e)
        {
            autoCloseTimer?.Stop();

            if (mainWindow != null && mainWindow.IsLoaded)
            {
                mainWindow.StartBreakFromNotification();
            }

            this.Close();
        }

        private void Dismiss_Click(object sender, RoutedEventArgs e)
        {
            autoCloseTimer?.Stop();
            this.Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            autoCloseTimer?.Stop();
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            autoCloseTimer?.Stop();
            autoCloseTimer = null;
        }
    }
}