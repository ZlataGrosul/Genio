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
            EventsModeBtn.IsChecked = true;
            ModeSelector.Margin = new Thickness(128, 0, 0, 0);
            UpdateFilterDateTexts();
            LoadAllOlimpsFromDatabase();
            LoadEventTypesToComboBox();
            FiltersComboBox.SelectionChanged += FiltersComboBox_SelectionChanged;
            SearchTextBox.TextChanged += SearchTextBox_TextChanged;
            ApplyAllFilters();
        }

        private void LoadAllOlimpsFromDatabase()
        {
            try
            {
                using (var context = new GenioAppEntities())
                {
                    allOlimps = context.Olimps
                        .Include("EventType")
                        .OrderByDescending(o => o.olimp_date)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка загрузки мероприятий: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
            }
        }

        private void ApplyAllFilters()
        {
            try
            {
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

                // 3. ✅ ИСПРАВЛЕНИЕ: Фильтр по дате с использованием .Date для корректного сравнения
                filteredOlimps = filteredOlimps
                    .Where(o => o.olimp_date.Date >= filterStartDate.Date &&
                               o.olimp_date.Date <= filterEndDate.Date)
                    .ToList();

                // Сортируем по дате
                filteredOlimps = filteredOlimps
                    .OrderByDescending(o => o.olimp_date)
                    .ToList();

                UpdateEventsList();

                if (filteredOlimps.Count > 0)
                {
                    if (selectedEventId > 0 && filteredOlimps.Any(o => o.olimp_id == selectedEventId))
                    {
                        SelectEvent(selectedEventId);
                    }
                    else
                    {
                        SelectEvent(filteredOlimps[0].olimp_id);
                    }
                }
                else
                {
                    StudentsList.Children.Clear();
                    selectedOlimp = null;
                    selectedEventId = 0;
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
                CustomMessageBox.Show($"Ошибка применения фильтров: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
            }
        }

        private void UpdateEventsList()
        {
            EventsList.Children.Clear();
            foreach (var olimp in filteredOlimps)
            {
                CreateEventCard(olimp);
            }
        }

        private void CreateEventCard(Olimp olimp)
        {
            var border = new Border();
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

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var nameTextBlock = new TextBlock
            {
                Text = olimp.olimp_name,
                Style = (Style)FindResource("CardTextStyle"),
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var dateTextBlock = new TextBlock
            {
                Text = olimp.olimp_date.ToString("dd.MM.yyyy"),
                Foreground = (SolidColorBrush)FindResource("WindowBackground"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };

            Grid.SetColumn(nameTextBlock, 0);
            Grid.SetColumn(dateTextBlock, 1);
            grid.Children.Add(nameTextBlock);
            grid.Children.Add(dateTextBlock);
            border.Child = grid;
            EventsList.Children.Add(border);
        }

        private void LoadEventTypesToComboBox()
        {
            try
            {
                using (var context = new GenioAppEntities())
                {
                    var eventTypes = context.EventTypes.ToList();
                    FiltersComboBox.Items.Clear();
                    FiltersComboBox.Items.Add(new ComboBoxItem { Content = "Все мероприятия", Tag = 0 });
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
                CustomMessageBox.Show($"Ошибка загрузки типов мероприятий: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
            }
        }

        private void LoadParticipantsForEvent(int eventId)
        {
            try
            {
                using (var context = new GenioAppEntities())
                {
                    eventParticipants = context.StudentOlimps
                        .Include("Student")
                        .Include("Student.Specialization")
                        .Where(so => so.olimp_id == eventId)
                        .ToList();

                    StudentsList.Children.Clear();
                    foreach (var participant in eventParticipants)
                    {
                        CreateParticipantCard(participant);
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка загрузки участников: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
            }
        }

        // ✅ ИСПРАВЛЕНИЕ: Добавлено отображение результата участника
        private void CreateParticipantCard(StudentOlimp participant)
        {
            var border = new Border();
            border.Style = (Style)FindResource("StudentCardColoredStyle");

            var cardBrushes = new[]
            {
                (SolidColorBrush)FindResource("StudentCardBrush1"),
                (SolidColorBrush)FindResource("StudentCardBrush2"),
                (SolidColorBrush)FindResource("StudentCardBrush3"),
                (SolidColorBrush)FindResource("StudentCardBrush4"),
                (SolidColorBrush)FindResource("StudentCardBrush5")
            };

            var random = new Random(participant.student_id);
            var brushIndex = random.Next(cardBrushes.Length);
            border.Background = cardBrushes[brushIndex];

            // ✅ ДОБАВЛЯЕМ РЕЗУЛЬТАТ В ТЕКСТ
            string resultText = !string.IsNullOrEmpty(participant.result) ? $" • {participant.result}" : "";

            var textBlock = new TextBlock
            {
                Text = $"{participant.Student.last_name} {participant.Student.first_name} {participant.Student.middle_name} • {participant.Student.group_name}{resultText}",
                Foreground = Brushes.White,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            border.Child = textBlock;
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
            currentSearchText = SearchTextBox.Text;
            ApplyAllFilters();
        }

        private void FiltersBtn_Click(object sender, RoutedEventArgs e)
        {
            filtersVisible = !filtersVisible;
            if (filtersVisible)
            {
                FiltersPanel.Visibility = Visibility.Visible;
                FiltersPanel.Margin = new Thickness(10, 0, 20, 15);
                StudentsSection.Margin = new Thickness(10, 0, 20, 225);
                StudentsSection.Height = 280;
                StudentsSection.VerticalAlignment = VerticalAlignment.Top;
                FiltersBtn.Style = (Style)FindResource("FiltersButtonActiveStyle");
                if (eventInfoVisible)
                {
                    eventInfoVisible = false;
                    EventInfoSection.Visibility = Visibility.Collapsed;
                    StudentsSection.Visibility = Visibility.Visible;
                }
            }
            else
            {
                FiltersPanel.Visibility = Visibility.Collapsed;
                StudentsSection.Margin = new Thickness(10, 0, 20, 15);
                StudentsSection.Height = Double.NaN;
                StudentsSection.VerticalAlignment = VerticalAlignment.Stretch;
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
                        mainWindow.LoadPage("Students");
                    }
                    else if (button.Name == "EventsModeBtn")
                    {
                        mainWindow.LoadPage("Olympiads");
                    }
                }
            }
        }

        private void ReportBtn_Click(object sender, RoutedEventArgs e)
        {
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
                    selectedEventId = eventId;
                    ShowEventInfo();
                }
                else
                {
                    SelectEvent(eventId);
                }
            }
        }

        private void SelectEvent(int eventId)
        {
            ResetEventSelection();
            foreach (Border border in EventsList.Children)
            {
                if (border.Tag != null && border.Tag.ToString() == eventId.ToString())
                {
                    border.Style = (Style)FindResource("SelectedEventCardStyle");
                    LoadSelectedEventInfo(eventId);
                    LoadParticipantsForEvent(eventId);
                    break;
                }
            }
            selectedEventId = eventId;
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
                CustomMessageBox.Show($"Ошибка загрузки информации о мероприятии: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
            }
        }

        private void ShowEventInfo()
        {
            if (selectedOlimp == null)
            {
                CustomMessageBox.Show("Выберите мероприятие", "Внимание",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Warning);
                return;
            }
            EventsModeBtn.IsChecked = true;
            RightSectionTitle.Text = "О мероприятии";
            StudentsSection.Visibility = Visibility.Collapsed;
            EventInfoSection.Visibility = Visibility.Visible;
            eventInfoVisible = true;
            LoadEventInfoToForm(selectedOlimp);
        }

        private void LoadEventInfoToForm(Olimp olimp)
        {
            EventDateValue.Text = olimp.olimp_date.ToString("dd.MM.yyyy");
            EventNameValue.Text = olimp.olimp_name;
            EventLevelValue.Text = olimp.olimp_level;
            EventTypeValue.Text = olimp.EventType?.type_name ?? "Не указано";
            EventNominationsValue.Text = olimp.nominations;
            EventLocationValue.Text = olimp.olimp_location;

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
            StudentsModeBtn.IsChecked = true;
            RightSectionTitle.Text = "Учащиеся";
            EventInfoSection.Visibility = Visibility.Collapsed;
            StudentsSection.Visibility = Visibility.Visible;
            eventInfoVisible = false;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.LoadPage("AddEditOlimp");
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedEventId > 0)
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    var page = new AddEditOlimpPage(selectedEventId);
                    mainWindow.MainFrame.Navigate(page);
                    mainWindow.LockMenu(true);
                }
            }
            else
            {
                CustomMessageBox.Show("Выберите мероприятие для редактирования", "Внимание",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Warning);
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
                // ✅ Применяем фильтры после выбора даты
                ApplyAllFilters();
            }
        }

        private void FiltersComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = FiltersComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem != null && selectedItem.Tag != null)
            {
                currentTypeFilter = (int)selectedItem.Tag;
                ApplyAllFilters();
            }
        }

        private void SearchTextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (SearchTextBox.Text == "Поиск...")
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = Brushes.White;
            }
        }

        private void ResetFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            currentSearchText = "";
            currentTypeFilter = 0;
            filterStartDate = DateTime.Now.AddYears(-3);
            filterEndDate = DateTime.Now.AddYears(1);
            SearchTextBox.Text = "Поиск...";
            SearchTextBox.Foreground = new SolidColorBrush(Color.FromArgb(255, 140, 140, 140));
            FiltersComboBox.SelectedIndex = 0;
            UpdateFilterDateTexts();
            ApplyAllFilters();
        }
    }
}