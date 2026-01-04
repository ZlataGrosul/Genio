using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Genio
{
    public partial class OlimpPage : Page
    {
        private bool filtersVisible = false;
        private bool eventInfoVisible = false;
        private TextBox currentDateTextBox = null;
        private DateTime filterStartDate = new DateTime(2024, 9, 1);
        private DateTime filterEndDate = new DateTime(2024, 12, 31);
        private int selectedEventId = 1;

        public OlimpPage()
        {
            InitializeComponent();

            // Инициализация текстовых полей дат
            UpdateFilterDateTexts();

            // Выделение первого мероприятия по умолчанию
            SelectEvent(1);
        }

        private void UpdateFilterDateTexts()
        {
            FilterDateFromText.Text = filterStartDate.ToString("dd.MM.yyyy");
            FilterDateToText.Text = filterEndDate.ToString("dd.MM.yyyy");
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == "Поиск...")
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = Brushes.White;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                SearchTextBox.Text = "Поиск...";
                SearchTextBox.Foreground = new SolidColorBrush(Color.FromArgb(255, 140, 140, 140));
            }
        }

        private void FiltersBtn_Click(object sender, RoutedEventArgs e)
        {
            filtersVisible = !filtersVisible;

            if (filtersVisible)
            {
                // Показываем панель фильтров
                FiltersPanel.Visibility = Visibility.Visible;
                FiltersPanel.Margin = new Thickness(10, 0, 20, 15); // обычный margin
                StudentsSection.Margin = new Thickness(10, 0, 20, 225); // увеличиваем нижний margin
                StudentsSection.Height = 280; // уменьшаем высоту фрейма учащихся
                StudentsSection.VerticalAlignment = VerticalAlignment.Top;

                // Меняем стиль кнопки фильтров на активный
                FiltersBtn.Style = (Style)FindResource("FiltersButtonActiveStyle");

                // Скрываем информацию о мероприятии если она открыта
                if (eventInfoVisible)
                {
                    eventInfoVisible = false;
                    EventInfoSection.Visibility = Visibility.Collapsed;
                    StudentsSection.Visibility = Visibility.Visible;
                }
            }
            else
            {
                // Скрываем панель фильтров
                FiltersPanel.Visibility = Visibility.Collapsed;
                StudentsSection.Margin = new Thickness(10, 0, 20, 15); // обычный margin
                StudentsSection.Height = Double.NaN; // сбрасываем высоту
                StudentsSection.VerticalAlignment = VerticalAlignment.Stretch;

                // Меняем стиль кнопки фильтров на обычный
                FiltersBtn.Style = (Style)FindResource("FiltersButtonStyle");
            }
        }

        private void ViewMode_Checked(object sender, RoutedEventArgs e)
        {
            // Анимация перемещения подсветки
            var button = sender as RadioButton;
            if (button != null)
            {
                if (button.Name == "StudentsModeBtn")
                {
                    // Показываем список учащихся
                    RightSectionTitle.Text = "Учащиеся";
                    StudentsSection.Visibility = Visibility.Visible;
                    EventInfoSection.Visibility = Visibility.Collapsed;

                    // Перемещаем подсветку
                    ModeSelector.Margin = new Thickness(2, 0, 0, 0);
                }
                else if (button.Name == "EventsModeBtn")
                {
                    // Показываем информацию о мероприятии
                    RightSectionTitle.Text = "Мероприятия";
                    StudentsSection.Visibility = Visibility.Collapsed;
                    EventInfoSection.Visibility = Visibility.Visible;

                    // Перемещаем подсветку
                    ModeSelector.Margin = new Thickness(128, 0, 0, 0);
                }
            }
        }

        private void ReportBtn_Click(object sender, RoutedEventArgs e)
        {
            // Переход на страницу отчетов
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                // Просто вызываем LoadPage, он сам позаботится о выделении меню
                mainWindow.LoadPage("Reports");
            }
        }

        private void EventItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                // Двойной клик - показываем информацию о мероприятии
                var border = sender as Border;
                if (border != null && border.Tag != null)
                {
                    int.TryParse(border.Tag.ToString(), out selectedEventId);
                    ShowEventInfo();
                }
            }
            else
            {
                // Одинарный клик - выделяем мероприятие
                var border = sender as Border;
                if (border != null && border.Tag != null)
                {
                    int.TryParse(border.Tag.ToString(), out selectedEventId);
                    SelectEvent(selectedEventId);
                }
            }
        }

        private void SelectEvent(int eventId)
        {
            // Сбрасываем выделение у всех мероприятий
            ResetEventSelection();

            // Выделяем выбранное мероприятие
            SolidColorBrush accentBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6155F5"));
            SolidColorBrush defaultBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF787878")); // CheckBoxBorder цвет

            switch (eventId)
            {
                case 1:
                    Event1.Background = accentBrush;
                    break;
                case 2:
                    Event2.Background = accentBrush;
                    break;
                case 3:
                    Event3.Background = accentBrush;
                    break;
                default:
                    Event1.Background = accentBrush;
                    break;
            }

            selectedEventId = eventId;
        }

        private void ResetEventSelection()
        {
            SolidColorBrush defaultBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF787878")); // CheckBoxBorder цвет

            Event1.Background = defaultBrush;
            Event2.Background = defaultBrush;
            Event3.Background = defaultBrush;
        }

        
        private void ShowEventInfo()
        {
            // Переключаемся в режим просмотра информации о мероприятии
            EventsModeBtn.IsChecked = true;
            RightSectionTitle.Text = "О мероприятии";
            StudentsSection.Visibility = Visibility.Collapsed;
            EventInfoSection.Visibility = Visibility.Visible;
            eventInfoVisible = true;

            // Загружаем данные о выбранном мероприятии
            LoadEventInfo(selectedEventId);
        }

        private void LoadEventInfo(int eventId)
        {
            // Здесь будет загрузка данных из БД
            // Пока используем тестовые данные
            switch (eventId)
            {
                case 1:
                    // Виртуальный бизнес-банк
                    break;
                case 2:
                    // Молодежь 21 века
                    break;
                case 3:
                    // Экономика XXI века
                    break;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Возвращаемся к списку учащихся
            StudentsModeBtn.IsChecked = true;
            RightSectionTitle.Text = "Учащиеся";
            EventInfoSection.Visibility = Visibility.Collapsed;
            StudentsSection.Visibility = Visibility.Visible;
            eventInfoVisible = false;
        }
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Режим добавления
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.LoadPage("AddEditOlimp");
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            // Режим редактирования (нужно выбрать мероприятие)
            if (selectedEventId > 0)
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    // Передаем ID выбранного мероприятия
                    var page = new AddEditOlimpPage(selectedEventId);
                    mainWindow.MainFrame.Navigate(page);
                    // Блокируем меню
                    mainWindow.LockMenu(true);
                }
            }
            else
            {
                MessageBox.Show("Выберите мероприятие для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void FilterDateFromButton_Click(object sender, RoutedEventArgs e)
        {
            currentDateTextBox = FilterDateFromText;
            ShowDatePicker(filterStartDate);
        }

        private void FilterDateToButton_Click(object sender, RoutedEventArgs e)
        {
            currentDateTextBox = FilterDateToText;
            ShowDatePicker(filterEndDate);
        }

        private void ShowDatePicker(DateTime initialDate)
        {
            DatePickerCalendar.SelectedDate = initialDate;
            DatePickerCalendar.DisplayDate = initialDate;

            // Настройка размещения popup
            if (currentDateTextBox == FilterDateFromText)
            {
                DatePickerPopup.PlacementTarget = FilterDateFromButton;
            }
            else
            {
                DatePickerPopup.PlacementTarget = FilterDateToButton;
            }

            DatePickerPopup.IsOpen = true;
        }

        private void DatePickerCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatePickerCalendar.SelectedDate.HasValue && currentDateTextBox != null)
            {
                var selectedDate = DatePickerCalendar.SelectedDate.Value;

                if (currentDateTextBox.Name == "FilterDateFromText")
                {
                    filterStartDate = selectedDate;
                }
                else if (currentDateTextBox.Name == "FilterDateToText")
                {
                    filterEndDate = selectedDate;
                }

                currentDateTextBox.Text = selectedDate.ToString("dd.MM.yyyy");
                DatePickerPopup.IsOpen = false;

                // Здесь можно добавить логику фильтрации по дате
            }
        }
    }
}