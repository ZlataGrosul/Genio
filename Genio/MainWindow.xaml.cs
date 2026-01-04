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
            // Подписка на события кнопок меню
            AnalyticsBtn.Checked += MenuButton_Checked;
            ReportsBtn.Checked += MenuButton_Checked;
            OlympiadsBtn.Checked += MenuButton_Checked;
            HonorBoardBtn.Checked += MenuButton_Checked;
            SettingsBtn.Checked += MenuButton_Checked;

            // По умолчанию открываем страницу по умолчанию
            AnalyticsBtn.IsChecked = true;
            LoadPage("Analytics");
        }

        private void MenuButton_Checked(object sender, RoutedEventArgs e)
        {
            var button = sender as RadioButton;
            if (button == null) return;

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

        private void LoadPage(string pageName)
        {
            switch (pageName)
            {
                case "Analytics":
                    // Показываем простую страницу аналитики
                    ShowSimplePage("Аналитика", "Страница аналитики находится в разработке");
                    break;

                case "Reports":
                    // Загружаем страницу отчетов
                    var reportsPage = new ReportsPage();
                    MainFrame.Navigate(reportsPage);
                    break;

                case "Olympiads":
                    ShowSimplePage("Олимпиады", "Страница олимпиад находится в разработке");
                    break;

                case "HonorBoard":
                    ShowSimplePage("Доска почета", "Страница доски почета находится в разработке");
                    break;

                case "Settings":
                    ShowSimplePage("Настройки", "Страница настроек находится в разработке");
                    break;
            }
        }

        private void ShowSimplePage(string title, string content)
        {
            // Создаем простую страницу для временного отображения
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
    }
}