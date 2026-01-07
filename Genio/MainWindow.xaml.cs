using System;
using System.Windows;
using System.Windows.Controls;

namespace Genio
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // подписка на события кнопок меню
            SubscribeMenuEvents();

            // по умолчанию открываем страницу по умолчанию
            LoadPage("Analytics");
        }

        private void SubscribeMenuEvents()
        {
            AnalyticsBtn.Checked += MenuButton_Checked;
            ReportsBtn.Checked += MenuButton_Checked;
            OlympiadsBtn.Checked += MenuButton_Checked;
            HonorBoardBtn.Checked += MenuButton_Checked;
            SettingsBtn.Checked += MenuButton_Checked;
        }

        private void UnsubscribeMenuEvents()
        {
            AnalyticsBtn.Checked -= MenuButton_Checked;
            ReportsBtn.Checked -= MenuButton_Checked;
            OlympiadsBtn.Checked -= MenuButton_Checked;
            HonorBoardBtn.Checked -= MenuButton_Checked;
            SettingsBtn.Checked -= MenuButton_Checked;
        }

        private void MenuButton_Checked(object sender, RoutedEventArgs e)
        {
            var button = sender as RadioButton;
            if (button == null || !button.IsChecked.HasValue || !button.IsChecked.Value) return;

            switch (button.Name)
            {
                case "AnalyticsBtn":
                    LoadPage("Analytics");
                    break;
                case "ReportsBtn":
                    LoadPage("Reports");
                    break;
                case "OlympiadsBtn":
                    LoadPage("Olympiads");
                    break;
                case "HonorBoardBtn":
                    LoadPage("HonorBoard");
                    break;
                case "SettingsBtn":
                    LoadPage("Settings");
                    break;
            }
        }

        public void LoadPage(string pageName)
        {
            // отписываемся от событий перед изменением состояния кнопок
            UnsubscribeMenuEvents();

            try
            {
                // блокируем меню для страницы редактирования
                if (pageName == "AddEditOlimp" || pageName == "AddEditStud")
                {
                    LockMenu(true);
                }
                else
                {
                    LockMenu(false);
                }

                // унимаем выделение со всех кнопок
                AnalyticsBtn.IsChecked = false;
                ReportsBtn.IsChecked = false;
                OlympiadsBtn.IsChecked = false;
                HonorBoardBtn.IsChecked = false;
                SettingsBtn.IsChecked = false;

                // устанавливаем выделение на нужной кнопке
                switch (pageName)
                {
                    case "Analytics":
                        AnalyticsBtn.IsChecked = true;
                        break;
                    case "Reports":
                        ReportsBtn.IsChecked = true;
                        break;
                    case "Olympiads":
                        OlympiadsBtn.IsChecked = true;
                        break;
                    case "HonorBoard":
                        HonorBoardBtn.IsChecked = true;
                        break;
                    case "Settings":
                        SettingsBtn.IsChecked = true;
                        break;
                }
            }
            finally
            {
                // подписываемся обратно
                SubscribeMenuEvents();
            }

            switch (pageName)
            {
                case "Analytics":
                    // показываем простую страницу аналитики
                    ShowSimplePage("Аналитика", "Страница аналитики находится в разработке");
                    break;

                case "Reports":
                    // загружаем страницу отчетов
                    var reportsPage = new ReportsPage();
                    MainFrame.Navigate(reportsPage);
                    break;

                case "Olympiads":
                    // загружаем страницу олимпиад
                    var olimpPage = new OlimpPage();
                    MainFrame.Navigate(olimpPage);
                    break;

                case "HonorBoard":
                    ShowSimplePage("Доска почета", "Страница доски почета находится в разработке");
                    break;

                case "Settings":
                    ShowSimplePage("Настройки", "Страница настроек находится в разработке");
                    break;

                case "AddEditOlimp":
                    // загружаем страницу добавления/редактирования олимпиад
                    var addEditPage = new AddEditOlimpPage();
                    MainFrame.Navigate(addEditPage);
                    break;

                case "AddEditStud":
                    // загружаем страницу добавления учащегося
                    var addEditStudPage = new AddEditStudPage(false);
                    MainFrame.Navigate(addEditStudPage);
                    break;

                case "AddEditStudEdit":
                    // загружаем страницу редактирования учащегося
                    var addEditStudPageEdit = new AddEditStudPage(true);
                    MainFrame.Navigate(addEditStudPageEdit);
                    break;
            }
        }

        private void ShowSimplePage(string title, string content)
        {
            // создаем простую страницу для временного отображения
            var page = new Page
            {
                Background = (System.Windows.Media.Brush)FindResource("WindowBackground")
            };

            var stackPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var titleText = new TextBlock
            {
                Text = title,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = (System.Windows.Media.Brush)FindResource("LightTextColor"),
                Margin = new Thickness(0, 0, 0, 20),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var contentText = new TextBlock
            {
                Text = content,
                FontSize = 16,
                Foreground = (System.Windows.Media.Brush)FindResource("LightTextColor"),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            stackPanel.Children.Add(titleText);
            stackPanel.Children.Add(contentText);
            page.Content = stackPanel;

            MainFrame.Navigate(page);
        }

        public void LockMenu(bool isLocked)
        {
            AnalyticsBtn.IsEnabled = !isLocked;
            ReportsBtn.IsEnabled = !isLocked;
            OlympiadsBtn.IsEnabled = !isLocked;
            HonorBoardBtn.IsEnabled = !isLocked;
            SettingsBtn.IsEnabled = !isLocked;
        }
    }
}