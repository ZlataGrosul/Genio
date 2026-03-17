using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.ObjectModel;

namespace Genio
{
    public class TempEventData
    {
        public string EventName { get; set; }
        public string EventDate { get; set; }
        public string EventLevel { get; set; }
        public int EventTypeId { get; set; }
        public string Nominations { get; set; }
        public string Location { get; set; }
        public List<TempParticipant> Participants { get; set; }

        public TempEventData()
        {
            Participants = new List<TempParticipant>();
        }
    }

    public class TempParticipant
    {
        public int StudentId { get; set; }
        public string FullName { get; set; }
        public string Group { get; set; }
        public string Result { get; set; }
    }

    public partial class AddEditOlimpPage : Page
    {
        private bool isEditMode = false;
        private int eventId = 0;
        private List<Student> allStudents = new List<Student>();
        private ObservableCollection<StudentOlimp> studentParticipations = new ObservableCollection<StudentOlimp>();
        private List<EventType> eventTypes = new List<EventType>();
        private Olimp currentOlimp = null;
        private TextBox currentDateTextBox = null;
        private Student selectedStudentForResult = null;
        private static TempEventData tempData = new TempEventData();

        public AddEditOlimpPage()
        {
            InitializeComponent();
            Loaded += AddEditOlimpPage_Loaded;
        }

        public AddEditOlimpPage(int eventId) : this()
        {
            this.eventId = eventId;
            this.isEditMode = true;
        }

        private void AddEditOlimpPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDataFromDatabase();
            if (isEditMode && eventId > 0)
            {
                DeleteButton.IsEnabled = true;
                LoadEventData(eventId);
                LoadSelectedStudentsForEvent(eventId);
            }
            else
            {
                DeleteButton.IsEnabled = false;
                LoadTempData();
            }

            StudentSearchTextBox.GotFocus += StudentSearchTextBox_GotFocus;
            StudentSearchTextBox.LostFocus += StudentSearchTextBox_LostFocus;
            StudentSearchTextBox.TextChanged += StudentSearchTextBox_TextChanged;
        }

        private void SaveTempData()
        {
            tempData.EventName = EventNameTextBox.Text;
            tempData.EventDate = EventDateTextBox.Text;
            tempData.Nominations = EventNominationsTextBox.Text;
            tempData.Location = EventLocationTextBox.Text;

            var levelItem = EventLevelComboBox.SelectedItem as ComboBoxItem;
            if (levelItem != null)
                tempData.EventLevel = levelItem.Content.ToString();

            var typeItem = EventTypeComboBox.SelectedItem as ComboBoxItem;
            if (typeItem != null && typeItem.Tag != null)
                tempData.EventTypeId = (int)typeItem.Tag;

            tempData.Participants.Clear();
            foreach (var participation in studentParticipations)
            {
                if (participation.Student != null)
                {
                    tempData.Participants.Add(new TempParticipant
                    {
                        StudentId = participation.student_id,
                        FullName = $"{participation.Student.last_name} {participation.Student.first_name} {participation.Student.middle_name}",
                        Group = participation.Student.group_name,
                        Result = participation.result
                    });
                }
            }
        }

        private void LoadTempData()
        {
            EventNameTextBox.Text = tempData.EventName;
            EventDateTextBox.Text = tempData.EventDate;
            EventNominationsTextBox.Text = tempData.Nominations;
            EventLocationTextBox.Text = tempData.Location;

            if (!string.IsNullOrEmpty(tempData.EventLevel))
            {
                foreach (ComboBoxItem item in EventLevelComboBox.Items)
                {
                    if (item.Content.ToString() == tempData.EventLevel)
                    {
                        EventLevelComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            if (tempData.EventTypeId > 0)
            {
                foreach (ComboBoxItem item in EventTypeComboBox.Items)
                {
                    if (item.Tag != null && (int)item.Tag == tempData.EventTypeId)
                    {
                        EventTypeComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            studentParticipations.Clear();
            ParticipantResultsPanel.Children.Clear();
            foreach (var tempParticipant in tempData.Participants)
            {
                var student = allStudents.FirstOrDefault(s => s.student_id == tempParticipant.StudentId);
                if (student != null)
                {
                    var participation = new StudentOlimp
                    {
                        student_id = student.student_id,
                        Student = student,
                        result = tempParticipant.Result,
                        created_date = DateTime.Now
                    };
                    studentParticipations.Add(participation);
                    CreateParticipantCard(participation);
                }
            }
            LoadAllStudents();
        }

        private void ClearTempData()
        {
            tempData = new TempEventData();
        }

        private void LoadDataFromDatabase()
        {
            try
            {
                using (var context = new GenioAppEntities())
                {
                    allStudents = context.Students_GetAll();
                    eventTypes = context.EventTypes_GetAll();

                    EventTypeComboBox.Items.Clear();
                    foreach (var type in eventTypes)
                    {
                        EventTypeComboBox.Items.Add(new ComboBoxItem
                        {
                            Content = type.type_name,
                            Tag = type.event_type_id
                        });
                    }
                    if (EventTypeComboBox.Items.Count > 0)
                        EventTypeComboBox.SelectedIndex = 0;

                    EventLevelComboBox.Items.Clear();
                    var levels = new[] { "Город", "Область", "Республика", "Международный" };
                    foreach (var level in levels)
                    {
                        EventLevelComboBox.Items.Add(new ComboBoxItem { Content = level });
                    }
                    if (EventLevelComboBox.Items.Count > 0)
                        EventLevelComboBox.SelectedIndex = 0;

                    LoadAllStudents();
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
            }
        }

        private void LoadAllStudents()
        {
            StudentsList.Children.Clear();
            foreach (var student in allStudents)
            {
                CreateStudentItem(student);
            }
        }

        private void CreateStudentItem(Student student)
        {
            bool isParticipant = studentParticipations.Any(sp => sp.student_id == student.student_id);
            var border = new Border();
            if (isParticipant)
                border.Style = (Style)FindResource("SelectedStudentItemStyle");
            else
                border.Style = (Style)FindResource("UnselectedStudentItemStyle");

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var textBlock = new TextBlock
            {
                Text = $"{student.last_name} {student.first_name} {student.middle_name} • {student.group_name}",
                Foreground = (SolidColorBrush)FindResource("LightTextColor"),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 10, 0)
            };

            var button = new Button
            {
                Width = 27,
                Height = 27,
                Tag = student,
                Margin = new Thickness(0, 0, 5, 0)
            };

            var image = new Image
            {
                Stretch = Stretch.Uniform,
                Width = 15,
                Height = 15
            };

            if (isParticipant)
            {
                button.Style = (Style)FindResource("DeleteListItemButtonStyle");
                image.Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Images/iconRemove.png"));
                button.ToolTip = "Удалить из участников";
            }
            else
            {
                button.Style = (Style)FindResource("StudentSelectionButtonStyle");
                image.Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Images/addIcon.png"));
                button.ToolTip = "Добавить к участникам";
            }

            button.Content = image;
            button.Click += StudentSelectionButton_Click;

            Grid.SetColumn(textBlock, 0);
            Grid.SetColumn(button, 1);
            grid.Children.Add(textBlock);
            grid.Children.Add(button);
            border.Child = grid;
            border.Tag = student;
            border.MouseLeftButtonDown += (s, e) => StudentItem_MouseLeftButtonDown(s, e, student);
            StudentsList.Children.Add(border);
        }

        private void StudentSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag is Student student)
            {
                var participation = studentParticipations.FirstOrDefault(sp => sp.student_id == student.student_id);
                if (participation != null)
                {
                    studentParticipations.Remove(participation);
                    RemoveParticipantCard(student.student_id);
                    if (selectedStudentForResult != null && selectedStudentForResult.student_id == student.student_id)
                    {
                        selectedStudentForResult = null;
                        StudentResultTextBox.Text = "";
                        StudentResultTextBox.IsEnabled = false;
                        AddResultButton.IsEnabled = false;
                    }
                    UpdateStudentItem(student, false);
                }
                else
                {
                    selectedStudentForResult = student;
                    StudentResultTextBox.IsEnabled = true;
                    StudentResultTextBox.Text = "";
                    AddResultButton.IsEnabled = true;
                    UpdateAllStudentItemsSelection();
                }
                e.Handled = true;
                SaveTempData();
            }
        }

        private void StudentItem_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e, Student student)
        {
            var participation = studentParticipations.FirstOrDefault(sp => sp.student_id == student.student_id);
            if (participation != null)
            {
                StudentResultTextBox.Text = participation.result;
                StudentResultTextBox.IsEnabled = true;
                AddResultButton.IsEnabled = true;
                selectedStudentForResult = student;
                UpdateAllStudentItemsSelection();
            }
            else
            {
                selectedStudentForResult = student;
                StudentResultTextBox.IsEnabled = true;
                StudentResultTextBox.Text = "";
                AddResultButton.IsEnabled = true;
                UpdateAllStudentItemsSelection();
            }
        }

        private void UpdateAllStudentItemsSelection()
        {
            foreach (var child in StudentsList.Children)
            {
                if (child is Border border && border.Child is Grid grid)
                {
                    var student = border.Tag as Student;
                    if (student != null)
                    {
                        bool isParticipant = studentParticipations.Any(sp => sp.student_id == student.student_id);
                        bool isSelected = selectedStudentForResult != null &&
                            selectedStudentForResult.student_id == student.student_id;

                        if (isParticipant || isSelected)
                            border.Style = (Style)FindResource("SelectedStudentItemStyle");
                        else
                            border.Style = (Style)FindResource("UnselectedStudentItemStyle");

                        var button = FindButtonInGrid(grid);
                        if (button != null)
                        {
                            var image = button.Content as Image;
                            if (image != null)
                            {
                                if (isParticipant)
                                {
                                    button.Style = (Style)FindResource("DeleteListItemButtonStyle");
                                    image.Source = new System.Windows.Media.Imaging.BitmapImage(
                                        new Uri("pack://application:,,,/Images/iconRemove.png"));
                                    button.ToolTip = "Удалить из участников";
                                }
                                else
                                {
                                    button.Style = (Style)FindResource("StudentSelectionButtonStyle");
                                    image.Source = new System.Windows.Media.Imaging.BitmapImage(
                                        new Uri("pack://application:,,,/Images/addIcon.png"));
                                    button.ToolTip = "Добавить к участникам";
                                }
                            }
                        }
                    }
                }
            }
        }

        private Button FindButtonInGrid(Grid grid)
        {
            foreach (var child in grid.Children)
            {
                if (child is Button button && Grid.GetColumn(button) == 1)
                    return button;
            }
            return null;
        }

        private void UpdateStudentItem(Student student, bool isParticipant)
        {
            foreach (var child in StudentsList.Children)
            {
                if (child is Border border && border.Tag is Student stud && stud.student_id == student.student_id)
                {
                    if (isParticipant)
                        border.Style = (Style)FindResource("SelectedStudentItemStyle");
                    else
                        border.Style = (Style)FindResource("UnselectedStudentItemStyle");

                    if (border.Child is Grid grid)
                    {
                        var button = FindButtonInGrid(grid);
                        if (button != null)
                        {
                            var image = button.Content as Image;
                            if (image != null)
                            {
                                if (isParticipant)
                                {
                                    button.Style = (Style)FindResource("DeleteListItemButtonStyle");
                                    image.Source = new System.Windows.Media.Imaging.BitmapImage(
                                        new Uri("pack://application:,,,/Images/iconRemove.png"));
                                    button.ToolTip = "Удалить из участников";
                                }
                                else
                                {
                                    button.Style = (Style)FindResource("StudentSelectionButtonStyle");
                                    image.Source = new System.Windows.Media.Imaging.BitmapImage(
                                        new Uri("pack://application:,,,/Images/addIcon.png"));
                                    button.ToolTip = "Добавить к участникам";
                                }
                            }
                        }
                    }
                    break;
                }
            }
        }

        private void AddResultButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedStudentForResult == null || string.IsNullOrWhiteSpace(StudentResultTextBox.Text))
            {
                CustomMessageBox.Show("Выберите студента и введите результат", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            var existingParticipation = studentParticipations.FirstOrDefault(sp => sp.student_id == selectedStudentForResult.student_id);
            if (existingParticipation != null)
            {
                existingParticipation.result = StudentResultTextBox.Text;
                UpdateParticipantCard(existingParticipation);
            }
            else
            {
                var participation = new StudentOlimp
                {
                    student_id = selectedStudentForResult.student_id,
                    Student = selectedStudentForResult,
                    result = StudentResultTextBox.Text,
                    created_date = DateTime.Now
                };
                studentParticipations.Add(participation);
                CreateParticipantCard(participation);
                UpdateStudentItem(selectedStudentForResult, true);
            }

            selectedStudentForResult = null;
            StudentResultTextBox.Text = "";
            StudentResultTextBox.IsEnabled = false;
            AddResultButton.IsEnabled = false;
            UpdateAllStudentItemsSelection();
            SaveTempData();
        }

        private void CreateParticipantCard(StudentOlimp participation)
        {
            var border = new Border { Style = (Style)FindResource("ParticipantResultItemStyle") };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var mainStackPanel = new StackPanel { Orientation = Orientation.Vertical };

            var resultTextBlock = new TextBlock
            {
                Text = participation.result,
                Foreground = (SolidColorBrush)FindResource("LightTextColor"),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 2)
            };

            string shortName = "";
            if (participation.Student != null)
            {
                shortName = GetShortName(participation.Student);
            }
            else
            {
                var student = allStudents.FirstOrDefault(s => s.student_id == participation.student_id);
                if (student != null)
                {
                    shortName = GetShortName(student);
                    participation.Student = student;
                }
            }

            var studentTextBlock = new TextBlock
            {
                Text = shortName,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 155, 155, 255)),
                FontSize = 12,
                FontStyle = FontStyles.Italic
            };

            mainStackPanel.Children.Add(resultTextBlock);
            mainStackPanel.Children.Add(studentTextBlock);

            var deleteButton = new Button
            {
                Style = (Style)FindResource("DeleteButtonStyle"),
                Tag = participation.student_id,
                Margin = new Thickness(10, 0, 0, 0),
                ToolTip = "Удалить результат"
            };

            var deleteImage = new Image
            {
                Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Images/iconRemove.png")),
                Width = 15,
                Height = 15,
                Stretch = Stretch.Uniform
            };

            deleteButton.Content = deleteImage;
            deleteButton.Click += DeleteParticipantButton_Click;

            Grid.SetColumn(mainStackPanel, 0);
            Grid.SetColumn(deleteButton, 1);
            grid.Children.Add(mainStackPanel);
            grid.Children.Add(deleteButton);
            border.Child = grid;
            border.Tag = participation.student_id;
            ParticipantResultsPanel.Children.Add(border);
        }

        private string GetShortName(Student student)
        {
            if (student == null) return "";
            if (!string.IsNullOrWhiteSpace(student.middle_name))
                return $"{student.last_name} {student.first_name[0]}.{student.middle_name[0]}.";
            else
                return $"{student.last_name} {student.first_name[0]}.";
        }

        private void UpdateParticipantCard(StudentOlimp participation)
        {
            foreach (var child in ParticipantResultsPanel.Children)
            {
                if (child is Border border && border.Tag is int studentId && studentId == participation.student_id)
                {
                    if (border.Child is Grid grid && grid.Children[0] is StackPanel stackPanel)
                    {
                        var resultTextBlock = stackPanel.Children[0] as TextBlock;
                        if (resultTextBlock != null)
                            resultTextBlock.Text = participation.result;
                    }
                    break;
                }
            }
        }

        private void RemoveParticipantCard(int studentId)
        {
            foreach (var child in ParticipantResultsPanel.Children)
            {
                if (child is Border border && border.Tag is int id && id == studentId)
                {
                    ParticipantResultsPanel.Children.Remove(border);
                    break;
                }
            }
        }

        private void DeleteParticipantButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag is int studentId)
            {
                var participation = studentParticipations.FirstOrDefault(sp => sp.student_id == studentId);
                if (participation != null)
                    studentParticipations.Remove(participation);

                RemoveParticipantCard(studentId);
                var student = allStudents.FirstOrDefault(s => s.student_id == studentId);
                if (student != null)
                    UpdateStudentItem(student, false);

                if (selectedStudentForResult != null && selectedStudentForResult.student_id == studentId)
                {
                    selectedStudentForResult = null;
                    StudentResultTextBox.Text = "";
                    StudentResultTextBox.IsEnabled = false;
                    AddResultButton.IsEnabled = false;
                }
                SaveTempData();
            }
        }

        private void LoadEventData(int eventId)
        {
            try
            {
                using (var context = new GenioAppEntities())
                {
                    currentOlimp = context.Olimps_GetById(eventId);
                    if (currentOlimp != null)
                    {
                        EventDateTextBox.Text = currentOlimp.olimp_date.ToString("dd.MM.yyyy");
                        EventNameTextBox.Text = currentOlimp.olimp_name;

                        var levelIndex = GetLevelIndex(currentOlimp.olimp_level);
                        if (levelIndex >= 0)
                            EventLevelComboBox.SelectedIndex = levelIndex;

                        var typeIndex = GetEventTypeIndex(currentOlimp.event_type_id);
                        if (typeIndex >= 0)
                            EventTypeComboBox.SelectedIndex = typeIndex;

                        EventNominationsTextBox.Text = currentOlimp.nominations;
                        EventLocationTextBox.Text = currentOlimp.olimp_location;
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка загрузки данных мероприятия: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
            }
        }

        private void LoadSelectedStudentsForEvent(int eventId)
        {
            try
            {
                using (var context = new GenioAppEntities())
                {
                    var participants = context.StudentOlimps_GetByOlimpId(eventId);

                    studentParticipations.Clear();
                    ParticipantResultsPanel.Children.Clear();

                    foreach (var participation in participants)
                    {
                        studentParticipations.Add(participation);
                        CreateParticipantCard(participation);
                    }
                    LoadAllStudents();
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка загрузки участников: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
            }
        }

        private int GetLevelIndex(string level)
        {
            var levels = new[] { "Город", "Область", "Республика", "Международный" };
            return Array.IndexOf(levels, level);
        }

        private int GetEventTypeIndex(int typeId)
        {
            for (int i = 0; i < EventTypeComboBox.Items.Count; i++)
            {
                var item = EventTypeComboBox.Items[i] as ComboBoxItem;
                if (item != null && (int)item.Tag == typeId)
                    return i;
            }
            return -1;
        }

        private void SetDefaultValues()
        {
            EventDateTextBox.Text = DateTime.Now.ToString("dd.MM.yyyy");
            EventNameTextBox.Text = "";
            if (EventLevelComboBox.Items.Count > 0)
                EventLevelComboBox.SelectedIndex = 0;
            if (EventTypeComboBox.Items.Count > 0)
                EventTypeComboBox.SelectedIndex = 0;
            EventNominationsTextBox.Text = "";
            EventLocationTextBox.Text = "";
            studentParticipations.Clear();
            ParticipantResultsPanel.Children.Clear();
            StudentResultTextBox.Text = "";
            StudentResultTextBox.IsEnabled = false;
            AddResultButton.IsEnabled = false;
        }

        private void EventDateButton_Click(object sender, RoutedEventArgs e)
        {
            currentDateTextBox = EventDateTextBox;
            ShowDatePicker(DateTime.Now);
        }

        private void ShowDatePicker(DateTime initialDate)
        {
            DatePickerCalendar.SelectedDate = initialDate;
            DatePickerCalendar.DisplayDate = initialDate;
            DatePickerPopup.PlacementTarget = EventDateButton;
            DatePickerPopup.IsOpen = true;
        }

        private void DatePickerCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatePickerCalendar.SelectedDate.HasValue && currentDateTextBox != null)
            {
                var selectedDate = DatePickerCalendar.SelectedDate.Value;
                currentDateTextBox.Text = selectedDate.ToString("dd.MM.yyyy");
                DatePickerPopup.IsOpen = false;
                SaveTempData();
            }
        }

        private void StudentSearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (StudentSearchTextBox.Text == "Поиск...")
            {
                StudentSearchTextBox.Text = "";
                StudentSearchTextBox.Foreground = Brushes.White;
            }
        }

        private void StudentSearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(StudentSearchTextBox.Text))
            {
                StudentSearchTextBox.Text = "Поиск...";
                StudentSearchTextBox.Foreground = new SolidColorBrush(Color.FromArgb(255, 140, 140, 140));
            }
            else
            {
                FilterStudents(StudentSearchTextBox.Text);
            }
        }

        private void StudentSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (StudentSearchTextBox.Text != "Поиск..." && !string.IsNullOrWhiteSpace(StudentSearchTextBox.Text))
            {
                FilterStudents(StudentSearchTextBox.Text);
            }
            else if (string.IsNullOrWhiteSpace(StudentSearchTextBox.Text))
            {
                LoadAllStudents();
            }
        }

        private void FilterStudents(string searchText)
        {
            StudentsList.Children.Clear();
            var filteredStudents = allStudents.Where(s =>
                s.last_name.ToLower().Contains(searchText.ToLower()) ||
                s.first_name.ToLower().Contains(searchText.ToLower()) ||
                s.middle_name.ToLower().Contains(searchText.ToLower()) ||
                s.group_name.ToLower().Contains(searchText.ToLower())).ToList();
            foreach (var student in filteredStudents)
            {
                CreateStudentItem(student);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ClearTempData();
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
                mainWindow.LoadPage("Olympiads");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EventNameTextBox.Text))
            {
                CustomMessageBox.Show("Введите название мероприятия", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Warning);
                EventNameTextBox.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(EventDateTextBox.Text))
            {
                CustomMessageBox.Show("Выберите дату мероприятия", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            try
            {
                int savedOlimpId = 0;
                using (var context = new GenioAppEntities())
                {
                    if (isEditMode && currentOlimp != null)
                    {
                        savedOlimpId = SaveEditedOlimp(context);
                    }
                    else
                    {
                        savedOlimpId = SaveNewOlimp(context);
                    }
                }

                if (savedOlimpId > 0)
                {
                    ClearTempData();
                    CustomMessageBox.Show(isEditMode ? "Изменения сохранены" : "Новое мероприятие добавлено",
                        isEditMode ? "Сохранение" : "Добавление",
                        CustomMessageBoxButton.OK, CustomMessageBoxIcon.Information);
                }

                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                    mainWindow.LoadPage("Olympiads");
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка сохранения данных: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
            }
        }

        private int SaveEditedOlimp(GenioAppEntities context)
        {
            // ✅ ИСПРАВЛЕНИЕ: Обновляем дату из текстового поля перед сохранением
            if (DateTime.TryParse(EventDateTextBox.Text, out DateTime newDate))
            {
                currentOlimp.olimp_date = newDate;
            }

            // Обновляем остальные поля
            currentOlimp.olimp_name = EventNameTextBox.Text.Trim();

            var levelItem = EventLevelComboBox.SelectedItem as ComboBoxItem;
            if (levelItem != null && !string.IsNullOrWhiteSpace(levelItem.Content.ToString()))
                currentOlimp.olimp_level = levelItem.Content.ToString().Trim();

            var typeItem = EventTypeComboBox.SelectedItem as ComboBoxItem;
            if (typeItem != null && typeItem.Tag != null)
                currentOlimp.event_type_id = (int)typeItem.Tag;

            currentOlimp.nominations = (EventNominationsTextBox.Text ?? "").Trim();
            currentOlimp.olimp_location = (EventLocationTextBox.Text ?? "").Trim();

            // Вызываем хранимую процедуру обновления
            context.Olimps_Update(
                currentOlimp.olimp_id,
                currentOlimp.olimp_name,
                currentOlimp.olimp_date,
                currentOlimp.event_type_id,
                currentOlimp.olimp_level,
                currentOlimp.olimp_location,
                currentOlimp.nominations);

            // Удаляем старых участников
            context.StudentOlimps_DeleteByOlimpId(currentOlimp.olimp_id);

            // Добавляем новых участников
            if (studentParticipations.Any())
            {
                foreach (var participation in studentParticipations)
                {
                    context.StudentOlimps_Insert(
                        participation.student_id,
                        currentOlimp.olimp_id,
                        participation.result);
                }
            }
            return currentOlimp.olimp_id;
        }

        private int SaveNewOlimp(GenioAppEntities context)
        {
            var newOlimp = new Olimp();
            UpdateOlimpData(newOlimp);

            int olimpId = context.Olimps_Insert(
                newOlimp.olimp_name,
                newOlimp.olimp_date,
                newOlimp.event_type_id,
                newOlimp.olimp_level,
                newOlimp.olimp_location,
                newOlimp.nominations);

            if (studentParticipations.Any())
            {
                foreach (var participation in studentParticipations)
                {
                    context.StudentOlimps_Insert(
                        participation.student_id,
                        olimpId,
                        participation.result);
                }
            }
            return olimpId;
        }

        private void UpdateOlimpData(Olimp olimp)
        {
            if (DateTime.TryParse(EventDateTextBox.Text, out DateTime date))
                olimp.olimp_date = date;
            else
            {
                olimp.olimp_date = DateTime.Now;
                EventDateTextBox.Text = olimp.olimp_date.ToString("dd.MM.yyyy");
            }
            olimp.olimp_name = EventNameTextBox.Text.Trim();

            var levelItem = EventLevelComboBox.SelectedItem as ComboBoxItem;
            if (levelItem != null && !string.IsNullOrWhiteSpace(levelItem.Content.ToString()))
                olimp.olimp_level = levelItem.Content.ToString().Trim();
            else
                olimp.olimp_level = "Город";

            var typeItem = EventTypeComboBox.SelectedItem as ComboBoxItem;
            if (typeItem != null && typeItem.Tag != null)
                olimp.event_type_id = (int)typeItem.Tag;
            else
                olimp.event_type_id = eventTypes.FirstOrDefault()?.event_type_id ?? 1;

            olimp.nominations = (EventNominationsTextBox.Text ?? "").Trim();
            olimp.olimp_location = (EventLocationTextBox.Text ?? "").Trim();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (isEditMode && eventId > 0)
            {
                var result = CustomMessageBox.Show("Вы уверены, что хотите удалить это мероприятие?",
                    "Подтверждение удаления",
                    CustomMessageBoxButton.YesNo, CustomMessageBoxIcon.Warning);

                if (result == CustomMessageBoxResult.Yes)
                {
                    try
                    {
                        using (var context = new GenioAppEntities())
                        {
                            context.Olimps_Delete(eventId);

                            CustomMessageBox.Show("Мероприятие удалено", "Удаление",
                                CustomMessageBoxButton.OK, CustomMessageBoxIcon.Information);
                            ClearTempData();

                            var mainWindow = Window.GetWindow(this) as MainWindow;
                            if (mainWindow != null)
                                mainWindow.LoadPage("Olympiads");
                        }
                    }
                    catch (Exception ex)
                    {
                        CustomMessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                            CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
                    }
                }
            }
        }

        private void AddStudentButton_Click(object sender, RoutedEventArgs e)
        {
            SaveTempData();
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                var page = new AddEditStudPage(false, "AddEditOlimp");
                mainWindow.MainFrame.Navigate(page);
                mainWindow.LockMenu(true);
            }
        }
    }
}