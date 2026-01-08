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
                    // При нажатии на Олимпиады в меню показываем страницу мероприятий
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
                if (pageName == "AddEditOlimp" || pageName == "AddEditStud" || pageName == "AddEditStudEdit")
                {
                    LockMenu(true);
                }
                else
                {
                    LockMenu(false);
                }

                // снимаем выделение со всех кнопок меню
                AnalyticsBtn.IsChecked = false;
                ReportsBtn.IsChecked = false;
                OlympiadsBtn.IsChecked = false;
                HonorBoardBtn.IsChecked = false;
                SettingsBtn.IsChecked = false;

                // устанавливаем выделение на нужной кнопке меню
                switch (pageName)
                {
                    case "Analytics":
                        AnalyticsBtn.IsChecked = true;
                        break;
                    case "Reports":
                        ReportsBtn.IsChecked = true;
                        break;
                    case "Olympiads":
                    case "Students": // При загрузке Students также подсвечиваем Olympiads в меню
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
                    ShowSimplePage("Аналитика", "Страница аналитики находится в разработке");
                    break;

                case "Reports":
                    var reportsPage = new ReportsPage();
                    MainFrame.Navigate(reportsPage);
                    break;

                case "Olympiads":
                    var olimpPage = new OlimpPage();
                    MainFrame.Navigate(olimpPage);
                    break;

                case "Students":
                    var studentsPage = new StudentsPage();
                    MainFrame.Navigate(studentsPage);
                    break;

                case "HonorBoard":
                    try
                    {
                        var honorBoardPage = new HonorBoardPage();
                        MainFrame.Navigate(honorBoardPage);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка загрузки страницы Доска почета: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        ShowSimplePage("Доска почета", "Страница доски почета находится в разработке");
                    }
                    break;

                case "Settings":
                    // ЗАМЕНА: вместо заглушки используем нашу новую страницу настроек
                    try
                    {
                        var settingsPage = new SettingsPage();
                        MainFrame.Navigate(settingsPage);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка загрузки страницы Настройки: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        ShowSimplePage("Настройки", "Ошибка загрузки страницы настроек");
                    }
                    break;

                case "AddEditOlimp":
                    var addEditPage = new AddEditOlimpPage();
                    MainFrame.Navigate(addEditPage);
                    break;

                case "AddEditStud":
                    var addEditStudPage = new AddEditStudPage(false);
                    MainFrame.Navigate(addEditStudPage);
                    break;

                case "AddEditStudEdit":
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