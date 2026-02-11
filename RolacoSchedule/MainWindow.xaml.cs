using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace RolacoSchedule
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool FlashWindow(IntPtr hWnd, bool bInvert);

        [DllImport("user32.dll")]
        private static extern int FlashWindowEx(ref FLASHWINFO pwfi);

        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        private const uint FLASHW_ALL = 3;
        private const uint FLASHW_TIMERNOFG = 12;
        private const int SW_RESTORE = 9;

        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ContextMenuStrip contextMenu;

        private ObservableCollection<TaskItem> tasks = new ObservableCollection<TaskItem>();
        private DispatcherTimer timer;
        private TimeSpan timeLeft;
        private bool isTimerRunning = false;
        private TaskItem currentTask;

        private bool isBreakMode = false;
        private TimeSpan originalTimeLeft;
        private TaskItem originalTask;

        public event PropertyChangedEventHandler PropertyChanged;

        private bool timerActive = false;
        public bool TimerActive
        {
            get => timerActive;
            set
            {
                timerActive = value;
                OnPropertyChanged(nameof(TimerActive));
            }
        }

        public bool IsBreakMode
        {
            get => isBreakMode;
            set
            {
                isBreakMode = value;
                OnPropertyChanged(nameof(IsBreakMode));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            InitializeNotifyIcon();
            LoadTasks();
            InitializeTimer();

            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        private void InitializeNotifyIcon()
        {
            try
            {
                notifyIcon = new System.Windows.Forms.NotifyIcon();

                try
                {
                    var uri = new Uri("pack://application:,,,/logo.ico");
                    var stream = Application.GetResourceStream(uri);

                    if (stream != null)
                    {
                        using (var streamReader = stream.Stream)
                        {
                            notifyIcon.Icon = new System.Drawing.Icon(streamReader);
                        }
                    }
                    else
                    {
                        notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                    }
                }
                catch
                {
                    notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                }

                notifyIcon.Text = "Rolaco Schedule";
                notifyIcon.Visible = true;

                contextMenu = new System.Windows.Forms.ContextMenuStrip();

                var showItem = new System.Windows.Forms.ToolStripMenuItem("Show");
                showItem.Click += (s, e) => ShowWindow();

                var exitItem = new System.Windows.Forms.ToolStripMenuItem("Exit");
                exitItem.Click += (s, e) =>
                {
                    notifyIcon?.Dispose();
                    Application.Current.Shutdown();
                };

                contextMenu.Items.Add(showItem);
                contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
                contextMenu.Items.Add(exitItem);

                notifyIcon.ContextMenuStrip = contextMenu;

                notifyIcon.DoubleClick += (s, e) => ShowWindow();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotifyIcon error: {ex.Message}");
            }
        }

        private void ShowWindow()
        {
            try
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
                this.Topmost = true;
                this.Topmost = false;

                var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                SetForegroundWindow(handle);
            }
            catch { }
        }

        private void FlashWindowOnTaskbar()
        {
            try
            {
                var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;

                FLASHWINFO fInfo = new FLASHWINFO();
                fInfo.cbSize = (uint)Marshal.SizeOf(fInfo);
                fInfo.hwnd = handle;
                fInfo.dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG;
                fInfo.uCount = uint.MaxValue;
                fInfo.dwTimeout = 0;

                FlashWindowEx(ref fInfo);
            }
            catch { }
        }

        private void StopFlashingWindow()
        {
            try
            {
                var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                FLASHWINFO fInfo = new FLASHWINFO();
                fInfo.cbSize = (uint)Marshal.SizeOf(fInfo);
                fInfo.hwnd = handle;
                fInfo.dwFlags = 0;
                fInfo.uCount = 0;

                FlashWindowEx(ref fInfo);
            }
            catch { }
        }

        private void ShowForcedNotification()
        {
            try
            {
                SystemSounds.Exclamation.Play();
                SystemSounds.Hand.Play();

                notifyIcon.ShowBalloonTip(
                    10000,
                    "Time's Up! - Rolaco Schedule",
                    $"Your {(currentTask?.Title ?? "Task")} session has been completed!",
                    System.Windows.Forms.ToolTipIcon.Info
                );

                notificationOverlay.Visibility = Visibility.Visible;

                ShowWindow();

                FlashWindowOnTaskbar();

                this.Topmost = true;
                var topmostTimer = new DispatcherTimer();
                topmostTimer.Interval = TimeSpan.FromSeconds(3);
                topmostTimer.Tick += (s, e) =>
                {
                    this.Topmost = false;
                    topmostTimer.Stop();
                };
                topmostTimer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Notification error: {ex.Message}");
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            try
            {
                StopFlashingWindow();
                notifyIcon?.Dispose();
                contextMenu?.Dispose();
            }
            catch { }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            btnTasks_Click(null, null);
            RefreshTaskComboBox();
            StopFlashingWindow();
        }

        private void InitializeTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (timeLeft.TotalSeconds > 0)
            {
                timeLeft = timeLeft.Subtract(TimeSpan.FromSeconds(1));
                UpdateTimerDisplay();
            }
            else
            {
                timer.Stop();
                isTimerRunning = false;
                TimerActive = false;

                if (isBreakMode)
                {
                    isBreakMode = false;

                    if (originalTimeLeft.TotalSeconds > 0)
                    {
                        timeLeft = originalTimeLeft;
                        currentTask = originalTask;

                        UpdateTimerDisplay();
                        if (currentTask != null)
                        {
                            txtTimerTask.Text = $"Task: {currentTask.Title}";
                        }

                        notifyIcon.ShowBalloonTip(
                            5000,
                            "Break Ended",
                            "Your break is over! Ready to continue?",
                            System.Windows.Forms.ToolTipIcon.Info
                        );

                        notificationOverlay.Visibility = Visibility.Visible;
                        FlashWindowOnTaskbar();

                        timer.Start();
                        isTimerRunning = true;
                        TimerActive = true;
                    }

                    btnTakeBreak.IsEnabled = true;
                    btnTakeBreak.Opacity = 1;

                    btnStartTimer.Visibility = Visibility.Collapsed;
                    btnPauseTimer.Visibility = Visibility.Visible;
                    btnStopTimer.Visibility = Visibility.Visible;
                }
                else
                {
                    ShowForcedNotification();
                    txtTimerDisplay.Text = "45:00";

                    btnStartTimer.Visibility = Visibility.Visible;
                    btnPauseTimer.Visibility = Visibility.Collapsed;
                    btnStopTimer.Visibility = Visibility.Collapsed;
                }

                btnPauseTimer.Content = "PAUSE";
                btnPauseTimer.Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));
                btnPauseTimer.Foreground = Brushes.White;
            }
        }

        private void UpdateTimerDisplay()
        {
            txtTimerDisplay.Text = $"{(int)timeLeft.TotalMinutes:D2}:{timeLeft.Seconds:D2}";
            if (currentTask != null)
            {
                txtCurrentTimer.Text = $"{(int)timeLeft.TotalMinutes:D2}:{timeLeft.Seconds:D2}";
            }
        }

        public bool TaskExists(int id)
        {
            return tasks.Any(t => t.Id == id);
        }

        private void RefreshTaskComboBox()
        {
            try
            {
                if (cmbTasks != null)
                {
                    cmbTasks.ItemsSource = null;
                    cmbTasks.ItemsSource = tasks;
                    cmbTasks.SelectedValuePath = "Id";
                    cmbTasks.DisplayMemberPath = "Title";
                    cmbTasks.Items.Refresh();
                    cmbTasks.SelectedItem = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing ComboBox: {ex.Message}");
            }
        }

        private int GetMinutesFromComboBoxItem(object selectedItem)
        {
            if (selectedItem is ComboBoxItem item)
            {
                string content = item.Content.ToString();
                string numericPart = new string(content
                    .TakeWhile(c => char.IsDigit(c))
                    .ToArray());

                if (int.TryParse(numericPart, out int minutes))
                {
                    return minutes;
                }
            }
            return 45;
        }

        private void LoadTasks()
        {
            try
            {
                tasks.Clear();

                if (File.Exists("tasks.json"))
                {
                    string json = File.ReadAllText("tasks.json");

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        WriteIndented = true
                    };

                    var loadedTasks = JsonSerializer.Deserialize<List<TaskItem>>(json, options);

                    if (loadedTasks != null && loadedTasks.Count > 0)
                    {
                        foreach (var task in loadedTasks)
                        {
                            if (task.DurationMinutes == 0 && !string.IsNullOrEmpty(task.Duration))
                            {
                                string numericPart = new string(task.Duration
                                    .TakeWhile(c => char.IsDigit(c))
                                    .ToArray());

                                if (int.TryParse(numericPart, out int minutes))
                                {
                                    task.DurationMinutes = minutes;
                                }
                            }
                            tasks.Add(task);
                        }
                    }
                }

                taskList.ItemsSource = tasks;

                if (cmbTasks != null)
                {
                    RefreshTaskComboBox();
                }

                taskList.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading tasks: {ex.Message}");
            }
        }

        private void SaveTasks()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = null
                };
                string json = JsonSerializer.Serialize(tasks.ToList(), options);
                File.WriteAllText("tasks.json", json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving tasks: {ex.Message}");
            }
        }

        private void btnTasks_Click(object sender, RoutedEventArgs e)
        {
            viewTasks.Visibility = Visibility.Visible;
            viewTimer.Visibility = Visibility.Collapsed;
            btnTasks.Background = new SolidColorBrush(Color.FromRgb(42, 42, 42));
            btnTimer.Background = Brushes.Transparent;
            taskList.Items.Refresh();
        }

        private void btnTimer_Click(object sender, RoutedEventArgs e)
        {
            viewTasks.Visibility = Visibility.Collapsed;
            viewTimer.Visibility = Visibility.Visible;
            btnTimer.Background = new SolidColorBrush(Color.FromRgb(42, 42, 42));
            btnTasks.Background = Brushes.Transparent;

            RefreshTaskComboBox();
        }

        private void ShowAddTaskDialog(object sender, RoutedEventArgs e)
        {
            var dialog = new TaskDialog();
            if (dialog.ShowDialog() == true && dialog.Task != null)
            {
                tasks.Add(dialog.Task);
                SaveTasks();
                RefreshTaskComboBox();
                taskList.Items.Refresh();
            }
        }

        private void ShowEditTaskDialog(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                int id = (int)button.Tag;
                var task = tasks.FirstOrDefault(t => t.Id == id);

                if (task != null)
                {
                    var dialog = new TaskDialog(task);
                    if (dialog.ShowDialog() == true)
                    {
                        SaveTasks();
                        taskList.Items.Refresh();
                        RefreshTaskComboBox();
                    }
                }
            }
        }

        private void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                int id = (int)button.Tag;
                var task = tasks.FirstOrDefault(t => t.Id == id);

                if (task != null)
                {
                    var result = MessageBox.Show($"Are you sure you want to delete '{task.Title}'?",
                                               "Confirm Delete",
                                               MessageBoxButton.YesNo,
                                               MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        tasks.Remove(task);
                        SaveTasks();
                        RefreshTaskComboBox();
                        taskList.Items.Refresh();
                    }
                }
            }
        }

        private void StartTaskTimer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                int id = (int)button.Tag;
                var task = tasks.FirstOrDefault(t => t.Id == id);

                if (task != null)
                {
                    currentTask = task;
                    StartTimerFromTask(task);
                    btnTimer_Click(null, null);
                }
            }
        }

        private void StartTimerFromTask(TaskItem task)
        {
            try
            {
                int minutes = task.DurationMinutes;

                timeLeft = TimeSpan.FromMinutes(minutes);
                txtTimerDisplay.Text = $"{minutes:00}:00";

                foreach (ComboBoxItem item in cmbMinutes.Items)
                {
                    if (item.Content.ToString().StartsWith(minutes.ToString()))
                    {
                        cmbMinutes.SelectedItem = item;
                        break;
                    }
                }

                cmbTasks.SelectedItem = task;

                txtTimerTask.Text = $"Task: {task.Title}";
                txtCurrentTimer.Text = $"{minutes:00}:00";

                foreach (var t in tasks)
                {
                    t.IsActive = false;
                }
                task.IsActive = true;

                timer.Start();
                isTimerRunning = true;
                TimerActive = true;

                btnStartTimer.Visibility = Visibility.Collapsed;
                btnPauseTimer.Visibility = Visibility.Visible;
                btnStopTimer.Visibility = Visibility.Visible;

                btnTakeBreak.IsEnabled = true;
                btnTakeBreak.Opacity = 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting timer: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartTimer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!isTimerRunning)
                {
                    if (timeLeft.TotalSeconds == 0)
                    {
                        int minutes = GetMinutesFromComboBoxItem(cmbMinutes.SelectedItem);
                        timeLeft = TimeSpan.FromMinutes(minutes);
                    }

                    timer.Start();
                    isTimerRunning = true;
                    TimerActive = true;

                    btnStartTimer.Visibility = Visibility.Collapsed;
                    btnPauseTimer.Visibility = Visibility.Visible;
                    btnStopTimer.Visibility = Visibility.Visible;

                    btnTakeBreak.IsEnabled = true;
                    btnTakeBreak.Opacity = 1;

                    if (cmbTasks.SelectedItem is TaskItem selectedTask)
                    {
                        currentTask = selectedTask;
                        txtTimerTask.Text = $"Task: {selectedTask.Title}";
                        txtCurrentTimer.Text = $"{(int)timeLeft.TotalMinutes:D2}:{timeLeft.Seconds:D2}";

                        foreach (var t in tasks)
                        {
                            t.IsActive = false;
                        }
                        selectedTask.IsActive = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting timer: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PauseTimer_Click(object sender, RoutedEventArgs e)
        {
            if (isTimerRunning)
            {
                timer.Stop();
                isTimerRunning = false;
                TimerActive = false;
                btnPauseTimer.Content = "RESUME";
                btnPauseTimer.Background = new SolidColorBrush(Color.FromRgb(255, 215, 0));
                btnPauseTimer.Foreground = new SolidColorBrush(Color.FromRgb(10, 10, 10));
            }
            else
            {
                timer.Start();
                isTimerRunning = true;
                TimerActive = true;
                btnPauseTimer.Content = "PAUSE";
                btnPauseTimer.Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));
                btnPauseTimer.Foreground = Brushes.White;
            }
        }

        private void StopTimer_Click(object sender, RoutedEventArgs e)
        {
            if (isBreakMode)
            {
                isBreakMode = false;
                btnTakeBreak.IsEnabled = true;
                btnTakeBreak.Opacity = 1;
                originalTimeLeft = TimeSpan.Zero;
                originalTask = null;
            }

            timer.Stop();
            isTimerRunning = false;
            TimerActive = false;
            timeLeft = TimeSpan.Zero;
            txtTimerDisplay.Text = "45:00";
            txtCurrentTimer.Text = "00:00";

            btnStartTimer.Visibility = Visibility.Visible;
            btnPauseTimer.Visibility = Visibility.Collapsed;
            btnStopTimer.Visibility = Visibility.Collapsed;
            btnPauseTimer.Content = "PAUSE";
            btnPauseTimer.Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));
            btnPauseTimer.Foreground = Brushes.White;

            if (currentTask != null)
            {
                currentTask.IsActive = false;
                currentTask = null;
            }

            txtTimerTask.Text = "No active task";
            cmbTasks.SelectedItem = null;
        }

        private void TakeBreak_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isTimerRunning || timeLeft.TotalSeconds > 0)
                {
                    originalTimeLeft = timeLeft;
                    originalTask = currentTask;

                    if (isTimerRunning)
                    {
                        timer.Stop();
                    }

                    isBreakMode = true;
                    timeLeft = TimeSpan.FromMinutes(15);

                    txtTimerDisplay.Text = "15:00";
                    txtCurrentTimer.Text = "15:00";
                    txtTimerTask.Text = "Break Time - 15 minutes";

                    timer.Start();
                    isTimerRunning = true;
                    TimerActive = true;

                    btnStartTimer.Visibility = Visibility.Collapsed;
                    btnPauseTimer.Visibility = Visibility.Visible;
                    btnStopTimer.Visibility = Visibility.Visible;
                    btnPauseTimer.Content = "PAUSE";

                    btnTakeBreak.IsEnabled = false;
                    btnTakeBreak.Opacity = 0.5;

                    notifyIcon.ShowBalloonTip(
                        3000,
                        "Break Time",
                        "Take a 15-minute break. Your session will resume automatically.",
                        System.Windows.Forms.ToolTipIcon.Info
                    );
                }
                else
                {
                    MessageBox.Show("Please start a timer first to take a break.",
                                  "No Active Timer",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Break error: {ex.Message}");
            }
        }

        public void StartBreakFromNotification()
        {
            try
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(() => StartBreakFromNotification());
                    return;
                }

                if (isTimerRunning || timeLeft.TotalSeconds > 0)
                {
                    originalTimeLeft = timeLeft;
                    originalTask = currentTask;

                    if (isTimerRunning)
                    {
                        timer.Stop();
                    }

                    isBreakMode = true;
                    timeLeft = TimeSpan.FromMinutes(15);

                    txtTimerDisplay.Text = "15:00";
                    txtCurrentTimer.Text = "15:00";
                    txtTimerTask.Text = "Break Time - 15 minutes";

                    timer.Start();
                    isTimerRunning = true;
                    TimerActive = true;

                    btnStartTimer.Visibility = Visibility.Collapsed;
                    btnPauseTimer.Visibility = Visibility.Visible;
                    btnStopTimer.Visibility = Visibility.Visible;
                    btnPauseTimer.Content = "PAUSE";

                    btnTakeBreak.IsEnabled = false;
                    btnTakeBreak.Opacity = 0.5;

                    notifyIcon?.ShowBalloonTip(
                        3000,
                        "Break Time",
                        "Take a 15-minute break. Your session will resume automatically.",
                        System.Windows.Forms.ToolTipIcon.Info
                    );
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Break error from notification: {ex.Message}");
            }
        }

        private void CloseNotification_Click(object sender, RoutedEventArgs e)
        {
            notificationOverlay.Visibility = Visibility.Collapsed;
            StopFlashingWindow();
        }

        private void MinimizeWindow_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeWindow_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized ?
                WindowState.Normal : WindowState.Maximized;
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            notifyIcon?.Dispose();
            contextMenu?.Dispose();
            Application.Current.Shutdown();
        }

        private void DragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TaskItem : INotifyPropertyChanged
    {
        private int _id;
        private string _title;
        private string _description;
        private int _durationMinutes;
        private bool _isActive;

        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        public int DurationMinutes
        {
            get => _durationMinutes;
            set
            {
                _durationMinutes = value;
                OnPropertyChanged(nameof(DurationMinutes));
                OnPropertyChanged(nameof(Duration));
            }
        }

        public string Duration
        {
            get => $"{_durationMinutes} minutes";
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    string numericPart = new string(value
                        .TakeWhile(c => char.IsDigit(c))
                        .ToArray());

                    if (int.TryParse(numericPart, out int minutes))
                    {
                        DurationMinutes = minutes;
                    }
                }
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                OnPropertyChanged(nameof(IsActive));
            }
        }

        public override string ToString()
        {
            return Title ?? "Untitled Task";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}