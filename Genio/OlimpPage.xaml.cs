using System;
using System.Collections.Generic;
using System.Linq;
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
        private DateTime filterStartDate = DateTime.Now.AddYears(-3);
        private DateTime filterEndDate = DateTime.Now.AddYears(1);
        private int selectedEventId = 0;
        private Olimp selectedOlimp = null;
        private List<Olimp> allOlimps = new List<Olimp>();
        private List<Olimp> filteredOlimps = new List<Olimp>();
        private List<StudentOlimp> eventParticipants = new List<StudentOlimp>();
        private string currentSearchText = "";
        private int currentTypeFilter = 0;

        public OlimpPage()
        {
            InitializeComponent();
            Loaded += OlimpPage_Loaded;
        }

        private void OlimpPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Настраиваем переключатель для мероприятий
            EventsModeBtn.IsChecked = true;
            ModeSelector.Margin = new Thickness(128, 0, 0, 0);

            // Инициализация текстовых полей дат
            UpdateFilterDateTexts();

            // Загрузка данных из БД
            LoadAllOlimpsFromDatabase();
            LoadEventTypesToComboBox();

            // Назначаем обработчик для ComboBox
            FiltersComboBox.SelectionChanged += FiltersComboBox_SelectionChanged;
            
            // Назначаем обработчик для поиска
            SearchTextBox.TextChanged += SearchTextBox_TextChanged;

            // Применяем фильтры и показываем данные
            ApplyAllFilters();
        }

        private void LoadAllOlimpsFromDatabase()
        {
            try
            {
                using (var context = new GenioAppEntities())
                {
                    // Загружаем ВСЕ мероприятия с типами
                    allOlimps = context.Olimps
                        .Include("EventType")
                        .OrderByDescending(o => o.olimp_date)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки мероприятий: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyAllFilters()
        {
            try
            {
                // Начинаем со всех мероприятий
                filteredOlimps = new List<Olimp>(allOlimps);

                // 1. Фильтр по поисковому запросу
                if (!string.IsNullOrWhiteSpace(currentSearchText) && currentSearchText != "Поиск...")
                {
                    filteredOlimps = filteredOlimps
                        .Where(o => o.olimp_name.ToLower().Contains(currentSearchText.ToLower()) ||
                                   o.olimp_location.ToLower().Contains(currentSearchText.ToLower()) ||
                                   o.nominations.ToLower().Contains(currentSearchText.ToLower()))
                        .ToList();
                }

                // 2. Фильтр по типу мероприятия
                if (currentTypeFilter > 0)
                {
                    filteredOlimps = filteredOlimps
                        .Where(o => o.event_type_id == currentTypeFilter)
                        .ToList();
                }

                // 3. Фильтр по дате
                filteredOlimps = filteredOlimps
                    .Where(o => o.olimp_date >= filterStartDate && o.olimp_date <= filterEndDate)
                    .ToList();

                // Сортируем по дате
                filteredOlimps = filteredOlimps
                    .OrderByDescending(o => o.olimp_date)
                    .ToList();

                // Обновляем список мероприятий
                UpdateEventsList();

                // Выбираем первое мероприятие, если есть
                if (filteredOlimps.Count > 0)
                {
                    if (selectedEventId > 0 && filteredOlimps.Any(o => o.olimp_id == selectedEventId))
                    {
                        // Если ранее выбранное мероприятие проходит фильтр, оставляем его выбранным
                        SelectEvent(selectedEventId);
                    }
                    else
                    {
                        // Иначе выбираем первое мероприятие
                        SelectEvent(filteredOlimps[0].olimp_id);
                    }
                }
                else
                {
                    // Очищаем список участников и информацию о мероприятии
                    StudentsList.Children.Clear();
                    selectedOlimp = null;
                    selectedEventId = 0;
                    
                    // Показываем сообщение, если нет мероприятий
                    if (allOlimps.Count > 0)
                    {
                        EventsList.Children.Clear();
                        var messageText = new TextBlock
                        {
                            Text = "Нет мероприятий, соответствующих выбранным фильтрам",
                            Foreground = (SolidColorBrush)FindResource("LightTextColor"),
                            FontSize = 14,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            TextAlignment = TextAlignment.Center,
                            Margin = new Thickness(0, 20, 0, 0)
                        };
                        EventsList.Children.Add(messageText);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка применения фильтров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateEventsList()
        {
            // Очищаем список мероприятий
            EventsList.Children.Clear();

            // Создаем элементы интерфейса для каждого отфильтрованного мероприятия
            foreach (var olimp in filteredOlimps)
            {
                CreateEventCard(olimp);
            }
        }

        private void CreateEventCard(Olimp olimp)
        {
            // Создаем Border для карточки мероприятия
            var border = new Border();

            // Устанавливаем стиль (выбранный или обычный)
            if (olimp.olimp_id == selectedEventId)
            {
                border.Style = (Style)FindResource("SelectedEventCardStyle");
            }
            else
            {
                border.Style = (Style)FindResource("EventCardStyle");
            }

            border.Tag = olimp.olimp_id;
            border.MouseLeftButtonDown += EventItem_MouseLeftButtonDown;

            // Создаем Grid для содержимого
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Название мероприятия
            var nameTextBlock = new TextBlock
            {
                Text = olimp.olimp_name,
                Style = (Style)FindResource("CardTextStyle"),
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            // Дата мероприятия
            var dateTextBlock = new TextBlock
            {
                Text = olimp.olimp_date.ToString("dd.MM.yyyy"),
                Foreground = (SolidColorBrush)FindResource("WindowBackground"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };

            // Добавляем элементы в Grid
            Grid.SetColumn(nameTextBlock, 0);
            Grid.SetColumn(dateTextBlock, 1);

            grid.Children.Add(nameTextBlock);
            grid.Children.Add(dateTextBlock);

            border.Child = grid;

            // Добавляем в список мероприятий
            EventsList.Children.Add(border);
        }

        private void LoadEventTypesToComboBox()
        {
            try
            {
                using (var context = new GenioAppEntities())
                {
                    var eventTypes = context.EventTypes.ToList();

                    // Очищаем ComboBox
                    FiltersComboBox.Items.Clear();

                    // Добавляем пункт "Все мероприятия"
                    FiltersComboBox.Items.Add(new ComboBoxItem
                    {
                        Content = "Все мероприятия",
                        Tag = 0
                    });

                    // Добавляем типы мероприятий
                    foreach (var type in eventTypes)
                    {
                        FiltersComboBox.Items.Add(new ComboBoxItem
                        {
                            Content = type.type_name,
                            Tag = type.event_type_id
                        });
                    }

                    FiltersComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки типов мероприятий: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadParticipantsForEvent(int eventId)
        {
            try
            {
                using (var context = new GenioAppEntities())
                {
                    // Загружаем участников для выбранного мероприятия
                    eventParticipants = context.StudentOlimps
                        .Include("Student")
                        .Include("Student.Specialization")
                        .Where(so => so.olimp_id == eventId)
                        .ToList();

                    // Очищаем список учащихся
                    StudentsList.Children.Clear();

                    // Создаем элементы для каждого участника
                    foreach (var participant in eventParticipants)
                    {
                        CreateParticipantCard(participant);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки участников: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateParticipantCard(StudentOlimp participant)
        {
            // Создаем Border для карточки участника
            var border = new Border();

            // Устанавливаем стиль
            border.Style = (Style)FindResource("StudentCardColoredStyle");

            // Получаем все цвета для карточек из ресурсов
            var cardBrushes = new[]
            {
                (SolidColorBrush)FindResource("StudentCardBrush1"),
                (SolidColorBrush)FindResource("StudentCardBrush2"),
                (SolidColorBrush)FindResource("StudentCardBrush3"),
                (SolidColorBrush)FindResource("StudentCardBrush4"),
                (SolidColorBrush)FindResource("StudentCardBrush5")
            };

            // Используем ID студента для выбора цвета
            var random = new Random(participant.student_id);
            var brushIndex = random.Next(cardBrushes.Length);

            border.Background = cardBrushes[brushIndex];

            // Создаем TextBlock с информацией об участнике
            var textBlock = new TextBlock
            {
                Text = $"{participant.Student.last_name} {participant.Student.first_name} {participant.Student.middle_name} • {participant.Student.group_name}",
                Foreground = Brushes.White,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            border.Child = textBlock;

            // Добавляем в список учащихся
            StudentsList.Children.Add(border);
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

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Сохраняем текущий поисковый запрос
            currentSearchText = SearchTextBox.Text;
            
            // Применяем все фильтры (включая поиск)
            ApplyAllFilters();
        }

        private void FiltersBtn_Click(object sender, RoutedEventArgs e)
        {
            filtersVisible = !filtersVisible;

            if (filtersVisible)
            {
                // Показываем панель фильтров
                FiltersPanel.Visibility = Visibility.Visible;
                FiltersPanel.Margin = new Thickness(10, 0, 20, 15);
                StudentsSection.Margin = new Thickness(10, 0, 20, 225);
                StudentsSection.Height = 280;
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
                StudentsSection.Margin = new Thickness(10, 0, 20, 15);
                StudentsSection.Height = Double.NaN;
                StudentsSection.VerticalAlignment = VerticalAlignment.Stretch;

                // Меняем стиль кнопки фильтров на обычный
                FiltersBtn.Style = (Style)FindResource("FiltersButtonStyle");
            }
        }

        private void ViewMode_Checked(object sender, RoutedEventArgs e)
        {
            var button = sender as RadioButton;
            if (button != null)
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    if (button.Name == "StudentsModeBtn")
                    {
                        // Переходим на страницу Учащиеся
                        mainWindow.LoadPage("Students");
                    }
                    else if (button.Name == "EventsModeBtn")
                    {
                        // Переходим на страницу Мероприятия
                        // Уже находимся здесь, но обновляем состояние
                        mainWindow.LoadPage("Olympiads");
                    }
                }
            }
        }

        private void ReportBtn_Click(object sender, RoutedEventArgs e)
        {
            // Переход на страницу отчетов
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.LoadPage("Reports");
            }
        }

        private void EventItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border != null && border.Tag != null)
            {
                int.TryParse(border.Tag.ToString(), out int eventId);

                if (e.ClickCount == 2)
                {
                    // Двойной клик - показываем информацию о мероприятии
                    selectedEventId = eventId;
                    ShowEventInfo();
                }
                else
                {
                    // Одинарный клик - выделяем мероприятие
                    SelectEvent(eventId);
                }
            }
        }

        private void SelectEvent(int eventId)
        {
            // Сбрасываем выделение у всех мероприятий
            ResetEventSelection();

            // Находим и выделяем выбранное мероприятие
            foreach (Border border in EventsList.Children)
            {
                if (border.Tag != null && border.Tag.ToString() == eventId.ToString())
                {
                    border.Style = (Style)FindResource("SelectedEventCardStyle");

                    // Загружаем информацию о выбранном мероприятии
                    LoadSelectedEventInfo(eventId);

                    // Загружаем участников для этого мероприятия
                    LoadParticipantsForEvent(eventId);
                    break;
                }
            }

            selectedEventId = eventId;

            // Если открыта информация о мероприятии, обновляем ее
            if (eventInfoVisible && selectedOlimp != null)
            {
                LoadEventInfoToForm(selectedOlimp);
            }
        }

        private void ResetEventSelection()
        {
            foreach (Border border in EventsList.Children)
            {
                border.Style = (Style)FindResource("EventCardStyle");
            }
        }

        private void LoadSelectedEventInfo(int eventId)
        {
            try
            {
                using (var context = new GenioAppEntities())
                {
                    selectedOlimp = context.Olimps
                        .Include("EventType")
                        .FirstOrDefault(o => o.olimp_id == eventId);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки информации о мероприятии: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowEventInfo()
        {
            if (selectedOlimp == null)
            {
                MessageBox.Show("Выберите мероприятие", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Переключаемся в режим просмотра информации о мероприятии
            EventsModeBtn.IsChecked = true;
            RightSectionTitle.Text = "О мероприятии";
            StudentsSection.Visibility = Visibility.Collapsed;
            EventInfoSection.Visibility = Visibility.Visible;
            eventInfoVisible = true;

            // Загружаем данные о выбранном мероприятии в форму
            LoadEventInfoToForm(selectedOlimp);
        }

        private void LoadEventInfoToForm(Olimp olimp)
        {
            // Используем прямые ссылки на элементы из XAML
            EventDateValue.Text = olimp.olimp_date.ToString("dd.MM.yyyy");
            EventNameValue.Text = olimp.olimp_name;
            EventLevelValue.Text = olimp.olimp_level;
            EventTypeValue.Text = olimp.EventType?.type_name ?? "Не указано";
            EventNominationsValue.Text = olimp.nominations;
            EventLocationValue.Text = olimp.olimp_location;

            // Получаем результаты участников
            try
            {
                using (var context = new GenioAppEntities())
                {
                    var results = context.StudentOlimps
                        .Where(so => so.olimp_id == olimp.olimp_id)
                        .Select(so => so.result)
                        .Distinct()
                        .ToList();

                    if (results.Any())
                    {
                        EventResultValue.Text = string.Join(", ", results.Where(r => !string.IsNullOrEmpty(r)));
                    }
                    else
                    {
                        EventResultValue.Text = "Нет данных о результатах";
                    }
                }
            }
            catch (Exception ex)
            {
                EventResultValue.Text = $"Ошибка загрузки результатов: {ex.Message}";
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

                // Применяем все фильтры (включая дату)
                ApplyAllFilters();
            }
        }

        private void FiltersComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Обработчик фильтра по типу мероприятия
            var selectedItem = FiltersComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem != null && selectedItem.Tag != null)
            {
                currentTypeFilter = (int)selectedItem.Tag;
                
                // Применяем все фильтры (включая тип мероприятия)
                ApplyAllFilters();
            }
        }

        // Обработчик для очистки поиска при клике
        private void SearchTextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (SearchTextBox.Text == "Поиск...")
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = Brushes.White;
            }
        }
        
        // Обработчик для кнопки сброса фильтров
        private void ResetFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            // Сбрасываем все фильтры
            currentSearchText = "";
            currentTypeFilter = 0;
            filterStartDate = DateTime.Now.AddYears(-3);
            filterEndDate = DateTime.Now.AddYears(1);
            
            // Обновляем UI
            SearchTextBox.Text = "Поиск...";
            SearchTextBox.Foreground = new SolidColorBrush(Color.FromArgb(255, 140, 140, 140));
            FiltersComboBox.SelectedIndex = 0;
            UpdateFilterDateTexts();
            
            // Применяем фильтры
            ApplyAllFilters();
        }
    }
}