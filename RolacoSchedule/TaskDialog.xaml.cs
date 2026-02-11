using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace RolacoSchedule
{
    public partial class TaskDialog : Window
    {
        public TaskItem Task { get; private set; }
        public bool IsEditMode { get; private set; }

        public TaskDialog(TaskItem task = null)
        {
            InitializeComponent();

            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.Owner = Application.Current.MainWindow;
            this.Topmost = false;

            txtTitle.TextChanged += (s, e) =>
                TitleCounter.Text = $"{txtTitle.Text.Length}/50";

            txtDescription.TextChanged += (s, e) =>
                DescriptionCounter.Text = $"{txtDescription.Text.Length}/200";

            if (task != null)
            {
                IsEditMode = true;
                Task = task;

                HeaderText.Text = "Edit Task";
                SaveButton.Content = "UPDATE";

                txtTitle.Text = task.Title;
                txtDescription.Text = task.Description;

                try
                {
                    string minutes = task.DurationMinutes.ToString();
                    foreach (ComboBoxItem item in cmbDuration.Items)
                    {
                        if (item.Content.ToString().StartsWith(minutes))
                        {
                            cmbDuration.SelectedItem = item;
                            break;
                        }
                    }
                }
                catch { }
            }
            else
            {
                IsEditMode = false;
                Task = null;

                HeaderText.Text = "Add New Task";
                SaveButton.Content = "SAVE";

                txtTitle.Text = "";
                txtDescription.Text = "";
                cmbDuration.SelectedIndex = 2; 
            }

            this.Loaded += (s, e) =>
            {
                txtTitle.Focus();
                Keyboard.Focus(txtTitle);
            };
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                MessageBox.Show("Please enter a task title.", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (txtTitle.Text.Length > 50)
            {
                MessageBox.Show("Task title cannot exceed 50 characters.", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbDuration.SelectedItem == null)
            {
                MessageBox.Show("Please select a duration.", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedItem = cmbDuration.SelectedItem as ComboBoxItem;
            string durationText = selectedItem.Content.ToString();

            string numericPart = new string(durationText
                .TakeWhile(c => char.IsDigit(c))
                .ToArray());

            if (!int.TryParse(numericPart, out int minutes))
            {
                minutes = 45;
            }

            if (IsEditMode && Task != null)
            {
                Task.Title = txtTitle.Text.Trim();
                Task.Description = txtDescription.Text?.Trim() ?? "";
                Task.DurationMinutes = minutes;
                Task.IsActive = false;
            }
            else
            {
                Task = new TaskItem
                {
                    Id = GenerateId(),
                    Title = txtTitle.Text.Trim(),
                    Description = txtDescription.Text?.Trim() ?? "",
                    DurationMinutes = minutes,
                    IsActive = false
                };
            }

            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is Border || e.OriginalSource is TextBlock)
                this.DragMove();
        }

        private int GenerateId()
        {
            Random rand = new Random();
            int id;
            do
            {
                id = rand.Next(1000, 9999);
            } while (Application.Current.MainWindow is MainWindow mainWindow &&
                    mainWindow.TaskExists(id));
            return id;
        }
    }
}