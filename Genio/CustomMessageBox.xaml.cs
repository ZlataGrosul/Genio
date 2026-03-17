// CustomMessageBox.xaml.cs
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Genio
{
    public enum CustomMessageBoxButton
    {
        OK,
        OKCancel,
        YesNo,
        YesNoCancel
    }

    public enum CustomMessageBoxResult
    {
        None,
        OK,
        Cancel,
        Yes,
        No
    }

    public enum CustomMessageBoxIcon
    {
        None,
        Information,
        Warning,
        Error,
        Question
    }

    public partial class CustomMessageBox : Window
    {
        private CustomMessageBoxResult _result = CustomMessageBoxResult.None;

        public CustomMessageBox(string message, string title = "",
            CustomMessageBoxButton buttons = CustomMessageBoxButton.OK,
            CustomMessageBoxIcon icon = CustomMessageBoxIcon.None)
        {
            InitializeComponent();
            SetupMessageBox(message, title, buttons, icon);
        }

        private void SetupMessageBox(string message, string title,
            CustomMessageBoxButton buttons, CustomMessageBoxIcon icon)
        {
            TitleText.Text = string.IsNullOrEmpty(title) ? "Сообщение" : title;
            MessageText.Text = message;
            SetIcon(icon);
            CreateButtons(buttons);

            if (Application.Current.MainWindow != null)
                this.Owner = Application.Current.MainWindow;
        }

        private void SetIcon(CustomMessageBoxIcon icon)
        {
            string iconPath = null;

            switch (icon)
            {
                case CustomMessageBoxIcon.Information:
                    iconPath = "/Images/infoIcon.png";
                    break;
                case CustomMessageBoxIcon.Warning:
                    iconPath = "/Images/infoIcon.png";
                    break;
                case CustomMessageBoxIcon.Error:
                    iconPath = "/Images/infoIcon.png";
                    break;
                case CustomMessageBoxIcon.Question:
                    iconPath = "/Images/questionIcon.png";
                    break;
                default:
                    iconPath = null;
                    break;
            }

            if (!string.IsNullOrEmpty(iconPath))
            {
                try
                {
                    IconImage.Source = new BitmapImage(new Uri($"pack://application:,,,{iconPath}"));
                    IconImage.Visibility = Visibility.Visible;
                }
                catch
                {
                    IconImage.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                IconImage.Visibility = Visibility.Collapsed;
            }
        }

        private void CreateButtons(CustomMessageBoxButton buttons)
        {
            ButtonsPanel.Children.Clear();

            switch (buttons)
            {
                case CustomMessageBoxButton.OK:
                    AddButton("OK", CustomMessageBoxResult.OK, false);
                    break;
                case CustomMessageBoxButton.OKCancel:
                    AddButton("OK", CustomMessageBoxResult.OK, false);
                    AddButton("Отмена", CustomMessageBoxResult.Cancel, true);
                    break;
                case CustomMessageBoxButton.YesNo:
                    AddButton("Да", CustomMessageBoxResult.Yes, false);
                    AddButton("Нет", CustomMessageBoxResult.No, true);
                    break;
                case CustomMessageBoxButton.YesNoCancel:
                    AddButton("Да", CustomMessageBoxResult.Yes, false);
                    AddButton("Нет", CustomMessageBoxResult.No, false);
                    AddButton("Отмена", CustomMessageBoxResult.Cancel, true);
                    break;
            }
        }

        private void AddButton(string text, CustomMessageBoxResult result, bool isSecondary)
        {
            var button = new Button
            {
                Content = text,
                Margin = new Thickness(10, 0, 0, 0),
                Style = isSecondary
                    ? (Style)FindResource("MessageBoxButtonSecondaryStyle")
                    : (Style)FindResource("MessageBoxButtonStyle")
            };

            // Красная кнопка для "Нет" и "Удалить"
            if (text == "Нет" || text == "Удалить")
            {
                button.Style = (Style)FindResource("MessageBoxButtonDangerStyle");
            }

            button.Click += (s, e) =>
            {
                _result = result;
                DialogResult = true;
                Close();
            };

            ButtonsPanel.Children.Add(button);
        }

        public new CustomMessageBoxResult ShowDialog()
        {
            base.ShowDialog();
            return _result;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        public static CustomMessageBoxResult Show(string message, string title = "",
            CustomMessageBoxButton buttons = CustomMessageBoxButton.OK,
            CustomMessageBoxIcon icon = CustomMessageBoxIcon.None)
        {
            var box = new CustomMessageBox(message, title, buttons, icon);
            return box.ShowDialog();
        }

        public static CustomMessageBoxResult ShowQuestion(string message, string title = "Подтверждение")
        {
            return Show(message, title, CustomMessageBoxButton.YesNo, CustomMessageBoxIcon.Question);
        }

        public static CustomMessageBoxResult ShowWarning(string message, string title = "Предупреждение")
        {
            return Show(message, title, CustomMessageBoxButton.OK, CustomMessageBoxIcon.Warning);
        }

        public static CustomMessageBoxResult ShowError(string message, string title = "Ошибка")
        {
            return Show(message, title, CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
        }

        public static CustomMessageBoxResult ShowYesNoCancel(string message, string title = "Подтверждение")
        {
            return Show(message, title, CustomMessageBoxButton.YesNoCancel, CustomMessageBoxIcon.Question);
        }
    }
}