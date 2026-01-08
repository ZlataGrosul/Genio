using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.ObjectModel;

namespace Genio
{
    // Класс для хранения временных данных мероприятия
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

        // Статическое хранилище для временных данных
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
            // Загружаем данные из БД
            LoadDataFromDatabase();

            if (isEditMode && eventId > 0)
            {
                // Режим редактирования
                DeleteButton.IsEnabled = true;
                LoadEventData(eventId);
                LoadSelectedStudentsForEvent(eventId);
            }
            else
            {
                // Режим добавления - загружаем временные данные
                DeleteButton.IsEnabled = false;
                LoadTempData();
            }

            // Настройка обработчиков событий
            StudentSearchTextBox.GotFocus += StudentSearchTextBox_GotFocus;
            StudentSearchTextBox.LostFocus += StudentSearchTextBox_LostFocus;
            StudentSearchTextBox.TextChanged += StudentSearchTextBox_TextChanged;
        }

        private void SaveTempData()
        {
            // Сохраняем основные данные
            tempData.EventName = EventNameTextBox.Text;
            tempData.EventDate = EventDateTextBox.Text;
            tempData.Nominations = EventNominationsTextBox.Text;
            tempData.Location = EventLocationTextBox.Text;

            // Сохраняем выбранный уровень
            var levelItem = EventLevelComboBox.SelectedItem as ComboBoxItem;
            if (levelItem != null)
            {
                tempData.EventLevel = levelItem.Content.ToString();
            }

            // Сохраняем выбранный тип
            var typeItem = EventTypeComboBox.SelectedItem as ComboBoxItem;
            if (typeItem != null && typeItem.Tag != null)
            {
                tempData.EventTypeId = (int)typeItem.Tag;
            }

            // Сохраняем участников
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
            // Восстанавливаем основные данные
            EventNameTextBox.Text = tempData.EventName;
            EventDateTextBox.Text = tempData.EventDate;
            EventNominationsTextBox.Text = tempData.Nominations;
            EventLocationTextBox.Text = tempData.Location;

            // Восстанавливаем уровень
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

            // Восстанавливаем тип
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

            // Восстанавливаем участников
            studentParticipations.Clear();
            ParticipantResultsPanel.Children.Clear();

            foreach (var tempParticipant in tempData.Participants)
            {
                // Находим студента в базе
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

            // Обновляем список студентов
            LoadAllStudents();
        }

        private void ClearTempData()
        {
            // Очищаем временные данные
            tempData = new TempEventData();
        }

        private void LoadDataFromDatabase()
        {
            try
            {
                using (var context = new GenioAppEntities())
                {
                    // Загружаем всех студентов
                    allStudents = context.Students
                        .Include("Specialization")
                        .OrderBy(s => s.last_name)
                        .ThenBy(s => s.first_name)
                        .ToList();

                    // Загружаем типы мероприятий
                    eventTypes = context.EventTypes.ToList();

                    // Заполняем ComboBox типов мероприятий
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

                    // Заполняем ComboBox уровней
                    EventLevelComboBox.Items.Clear();
                    var levels = new[] { "Город", "Область", "Республика", "Международный" };
                    foreach (var level in levels)
                    {
                        EventLevelComboBox.Items.Add(new ComboBoxItem { Content = level });
                    }

                    if (EventLevelComboBox.Items.Count > 0)
                        EventLevelComboBox.SelectedIndex = 0;

                    // Загружаем список всех студентов
                    LoadAllStudents();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAllStudents()
        {
            // Очищаем список студентов
            StudentsList.Children.Clear();

            // Создаем элементы для каждого студента
            foreach (var student in allStudents)
            {
                CreateStudentItem(student);
            }
        }

        private void CreateStudentItem(Student student)
        {
            // Проверяем, добавлен ли уже этот студент как участник с результатом
            bool isParticipant = studentParticipations.Any(sp => sp.student_id == student.student_id);

            // Создаем Border для элемента
            var border = new Border();

            // Устанавливаем стиль в зависимости от участия
            if (isParticipant)
            {
                border.Style = (Style)FindResource("SelectedStudentItemStyle");
            }
            else
            {
                border.Style = (Style)FindResource("UnselectedStudentItemStyle");
            }

            // Создаем Grid для содержимого
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Текст с ФИО и группой
            var textBlock = new TextBlock
            {
                Text = $"{student.last_name} {student.first_name} {student.middle_name} • {student.group_name}",
                Foreground = (SolidColorBrush)FindResource("LightTextColor"),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 10, 0)
            };

            // Кнопка добавления/удаления
            var button = new Button
            {
                Style = (Style)FindResource("StudentSelectionButtonStyle"),
                Width = 27,
                Height = 27,
                Tag = student,
                Margin = new Thickness(0, 0, 5, 0)
            };

            // Иконка кнопки
            var image = new Image
            {
                Stretch = Stretch.Uniform,
                Width = 15,
                Height = 15
            };

            // Устанавливаем иконку в зависимости от состояния
            if (isParticipant)
            {
                image.Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Images/iconRemove.png"));
                button.ToolTip = "Удалить из участников";
            }
            else
            {
                image.Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Images/addIcon.png"));
                button.ToolTip = "Добавить к участникам";
            }

            button.Content = image;
            button.Click += StudentSelectionButton_Click;

            // Добавляем элементы в Grid
            Grid.SetColumn(textBlock, 0);
            Grid.SetColumn(button, 1);

            grid.Children.Add(textBlock);
            grid.Children.Add(button);

            border.Child = grid;
            border.Tag = student;
            border.Cursor = System.Windows.Input.Cursors.Hand;

            // Двойной клик по карточке тоже выбирает студента для добавления результата
            border.MouseLeftButtonDown += (s, e) => StudentItem_MouseLeftButtonDown(s, e, student);

            // Добавляем в список
            StudentsList.Children.Add(border);
        }

        private void StudentSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag is Student student)
            {
                // Проверяем, добавлен ли уже этот студент как участник
                var participation = studentParticipations.FirstOrDefault(sp => sp.student_id == student.student_id);

                if (participation != null)
                {
                    // Удаляем из участников
                    studentParticipations.Remove(participation);

                    // Удаляем карточку результата
                    RemoveParticipantCard(student.student_id);

                    // Сбрасываем выбранного студента, если это он
                    if (selectedStudentForResult != null && selectedStudentForResult.student_id == student.student_id)
                    {
                        selectedStudentForResult = null;
                        StudentResultTextBox.Text = "";
                        StudentResultTextBox.IsEnabled = false;
                        AddResultButton.IsEnabled = false;
                    }

                    // Обновляем отображение
                    UpdateStudentItem(student, false);
                }
                else
                {
                    // Если студент не участник, выбираем его для добавления результата
                    selectedStudentForResult = student;
                    StudentResultTextBox.IsEnabled = true;
                    StudentResultTextBox.Text = "";
                    AddResultButton.IsEnabled = true;

                    // Подсвечиваем выбранного студента
                    UpdateAllStudentItemsSelection();
                }

                e.Handled = true; // Предотвращаем всплытие события
            }

            // Сохраняем данные
            SaveTempData();
        }

        private void StudentItem_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e, Student student)
        {
            // Проверяем, является ли студент участником
            var participation = studentParticipations.FirstOrDefault(sp => sp.student_id == student.student_id);

            if (participation != null)
            {
                // Если студент уже участник, показываем его текущий результат
                StudentResultTextBox.Text = participation.result;
                StudentResultTextBox.IsEnabled = true;
                AddResultButton.IsEnabled = true;
                selectedStudentForResult = student;

                // Подсвечиваем выбранного студента
                UpdateAllStudentItemsSelection();
            }
            else
            {
                // Если студент не участник, выбираем его для добавления результата
                selectedStudentForResult = student;
                StudentResultTextBox.IsEnabled = true;
                StudentResultTextBox.Text = "";
                AddResultButton.IsEnabled = true;

                // Подсвечиваем выбранного студента
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

                        // Обновляем стиль Border
                        if (isParticipant || isSelected)
                        {
                            border.Style = (Style)FindResource("SelectedStudentItemStyle");
                        }
                        else
                        {
                            border.Style = (Style)FindResource("UnselectedStudentItemStyle");
                        }

                        // Обновляем кнопку
                        var button = FindButtonInGrid(grid);
                        if (button != null)
                        {
                            var image = button.Content as Image;
                            if (image != null)
                            {
                                if (isParticipant)
                                {
                                    image.Source = new System.Windows.Media.Imaging.BitmapImage(
                                        new Uri("pack://application:,,,/Images/iconRemove.png"));
                                    button.ToolTip = "Удалить из участников";
                                }
                                else
                                {
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
                {
                    return button;
                }
            }
            return null;
        }

        private void UpdateStudentItem(Student student, bool isParticipant)
        {
            foreach (var child in StudentsList.Children)
            {
                if (child is Border border && border.Tag is Student stud && stud.student_id == student.student_id)
                {
                    // Обновляем стиль
                    if (isParticipant)
                    {
                        border.Style = (Style)FindResource("SelectedStudentItemStyle");
                    }
                    else
                    {
                        border.Style = (Style)FindResource("UnselectedStudentItemStyle");
                    }

                    // Обновляем кнопку
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
                                    image.Source = new System.Windows.Media.Imaging.BitmapImage(
                                        new Uri("pack://application:,,,/Images/iconRemove.png"));
                                    button.ToolTip = "Удалить из участников";
                                }
                                else
                                {
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
                MessageBox.Show("Выберите студента и введите результат", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверяем, не добавлен ли уже этот студент
            var existingParticipation = studentParticipations.FirstOrDefault(sp => sp.student_id == selectedStudentForResult.student_id);

            if (existingParticipation != null)
            {
                // Обновляем результат существующего участника
                existingParticipation.result = StudentResultTextBox.Text;
                UpdateParticipantCard(existingParticipation);
            }
            else
            {
                // Создаем новый объект StudentOlimp
                var participation = new StudentOlimp
                {
                    student_id = selectedStudentForResult.student_id,
                    Student = selectedStudentForResult,
                    result = StudentResultTextBox.Text,
                    created_date = DateTime.Now
                };

                studentParticipations.Add(participation);
                CreateParticipantCard(participation);

                // Обновляем отображение студента в списке
                UpdateStudentItem(selectedStudentForResult, true);
            }

            // Сбрасываем выбранного студента и очищаем поле
            selectedStudentForResult = null;
            StudentResultTextBox.Text = "";
            StudentResultTextBox.IsEnabled = false;
            AddResultButton.IsEnabled = false;

            // Сбрасываем выделение всех студентов
            UpdateAllStudentItemsSelection();

            // Сохраняем данные
            SaveTempData();
        }

        private void CreateParticipantCard(StudentOlimp participation)
        {
            var border = new Border
            {
                Style = (Style)FindResource("ParticipantResultItemStyle")
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Основной текст с результатом и ФИО
            var mainStackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            var resultTextBlock = new TextBlock
            {
                Text = participation.result,
                Foreground = (SolidColorBrush)FindResource("LightTextColor"),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 2)
            };

            // Создаем сокращенное ФИО
            string shortName = GetShortName(participation.Student);

            var studentTextBlock = new TextBlock
            {
                Text = shortName,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 155, 155, 255)), // Светло-фиолетовый
                FontSize = 12,
                FontStyle = FontStyles.Italic
            };

            mainStackPanel.Children.Add(resultTextBlock);
            mainStackPanel.Children.Add(studentTextBlock);

            // Кнопка удаления из результатов
            var deleteButton = new Button
            {
                Style = (Style)FindResource("CircleButtonStyle"),
                Width = 50,
                Height = 50,
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

            // Добавляем элементы в Grid
            Grid.SetColumn(mainStackPanel, 0);
            Grid.SetColumn(deleteButton, 1);

            grid.Children.Add(mainStackPanel);
            grid.Children.Add(deleteButton);

            border.Child = grid;
            border.Tag = participation.student_id;

            // Добавляем карточку в список участников
            ParticipantResultsPanel.Children.Add(border);
        }

        private string GetShortName(Student student)
        {
            if (student == null) return "";

            if (!string.IsNullOrWhiteSpace(student.middle_name))
            {
                return $"{student.last_name} {student.first_name[0]}.{student.middle_name[0]}.";
            }
            else
            {
                return $"{student.last_name} {student.first_name[0]}.";
            }
        }

        private void UpdateParticipantCard(StudentOlimp participation)
        {
            // Находим существующую карточку и обновляем её
            foreach (var child in ParticipantResultsPanel.Children)
            {
                if (child is Border border && border.Tag is int studentId && studentId == participation.student_id)
                {
                    if (border.Child is Grid grid && grid.Children[0] is StackPanel stackPanel)
                    {
                        var resultTextBlock = stackPanel.Children[0] as TextBlock;
                        if (resultTextBlock != null)
                        {
                            resultTextBlock.Text = participation.result;
                        }
                    }
                    break;
                }
            }
        }

        private void RemoveParticipantCard(int studentId)
        {
            // Удаляем карточку из интерфейса
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
                // Удаляем из списка участников
                var participation = studentParticipations.FirstOrDefault(sp => sp.student_id == studentId);
                if (participation != null)
                {
                    studentParticipations.Remove(participation);
                }

                // Удаляем карточку из интерфейса
                RemoveParticipantCard(studentId);

                // Обновляем отображение студента в списке
                var student = allStudents.FirstOrDefault(s => s.student_id == studentId);
                if (student != null)
                {
                    UpdateStudentItem(student, false);
                }

                // Сбрасываем выбранного студента, если это он
                if (selectedStudentForResult != null && selectedStudentForResult.student_id == studentId)
                {
                    selectedStudentForResult = null;
                    StudentResultTextBox.Text = "";
                    StudentResultTextBox.IsEnabled = false;
                    AddResultButton.IsEnabled = false;
                }

                // Сохраняем данные
                SaveTempData();
            }
        }

        private void LoadEventData(int eventId)
        {
            try
            {
                using (var context = new GenioAppEntities())
                {
                    currentOlimp = context.Olimps
                        .Include("EventType")
                        .FirstOrDefault(o => o.olimp_id == eventId);

                    if (currentOlimp != null)
                    {
                        // Заполняем форму данными мероприятия
                        EventDateTextBox.Text = currentOlimp.olimp_date.ToString("dd.MM.yyyy");
                        EventNameTextBox.Text = currentOlimp.olimp_name;

                        // Устанавливаем уровень
                        var levelIndex = GetLevelIndex(currentOlimp.olimp_level);
                        if (levelIndex >= 0)
                            EventLevelComboBox.SelectedIndex = levelIndex;

                        // Устанавливаем тип мероприятия
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
                MessageBox.Show($"Ошибка загрузки данных мероприятия: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSelectedStudentsForEvent(int eventId)
        {
            try
            {
                using (var context = new GenioAppEntities())
                {
                    // Загружаем участников мероприятия с их результатами
                    var participants = context.StudentOlimps
                        .Include("Student")
                        .Where(so => so.olimp_id == eventId)
                        .ToList();

                    studentParticipations.Clear();
                    ParticipantResultsPanel.Children.Clear();

                    foreach (var participation in participants)
                    {
                        studentParticipations.Add(participation);
                        CreateParticipantCard(participation);
                    }

                    // Обновляем отображение списка студентов
                    LoadAllStudents();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки участников: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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

            // Устанавливаем уровень по умолчанию
            if (EventLevelComboBox.Items.Count > 0)
                EventLevelComboBox.SelectedIndex = 0;

            // Устанавливаем тип мероприятия по умолчанию
            if (EventTypeComboBox.Items.Count > 0)
                EventTypeComboBox.SelectedIndex = 0;

            EventNominationsTextBox.Text = "";
            EventLocationTextBox.Text = "";

            // Очищаем список участников
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

                // Сохраняем данные
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
            // Очищаем список студентов
            StudentsList.Children.Clear();

            // Фильтруем студентов по поисковому запросу
            var filteredStudents = allStudents.Where(s =>
                s.last_name.ToLower().Contains(searchText.ToLower()) ||
                s.first_name.ToLower().Contains(searchText.ToLower()) ||
                s.middle_name.ToLower().Contains(searchText.ToLower()) ||
                s.group_name.ToLower().Contains(searchText.ToLower())).ToList();

            // Создаем элементы для отфильтрованных студентов
            foreach (var student in filteredStudents)
            {
                CreateStudentItem(student);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Очищаем временные данные перед возвратом
            ClearTempData();

            // Возврат на страницу мероприятий
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.LoadPage("Olympiads");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Валидация данных
            if (string.IsNullOrWhiteSpace(EventNameTextBox.Text))
            {
                MessageBox.Show("Введите название мероприятия", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EventNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(EventDateTextBox.Text))
            {
                MessageBox.Show("Выберите дату мероприятия", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int savedOlimpId = 0;

                using (var context = new GenioAppEntities())
                {
                    if (isEditMode && currentOlimp != null)
                    {
                        // Режим редактирования
                        savedOlimpId = SaveEditedOlimp(context);
                    }
                    else
                    {
                        // Режим добавления
                        savedOlimpId = SaveNewOlimp(context);
                    }
                }

                if (savedOlimpId > 0)
                {
                    // Очищаем временные данные после успешного сохранения
                    ClearTempData();

                    MessageBox.Show(isEditMode ? "Изменения сохранены" : "Новое мероприятие добавлено",
                        isEditMode ? "Сохранение" : "Добавление",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Возврат на страницу мероприятий с очисткой временных данных
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.LoadPage("Olympiads");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int SaveEditedOlimp(GenioAppEntities context)
        {
            var olimp = context.Olimps.Find(currentOlimp.olimp_id);
            if (olimp == null) return 0;

            // Обновляем данные мероприятия
            UpdateOlimpData(olimp);

            // Удаляем старых участников
            var oldParticipants = context.StudentOlimps
                .Where(so => so.olimp_id == olimp.olimp_id)
                .ToList();
            context.StudentOlimps.RemoveRange(oldParticipants);

            // Сохраняем изменения мероприятия
            context.SaveChanges();

            // Добавляем новых участников
            if (studentParticipations.Any())
            {
                AddParticipantsToOlimp(context, olimp.olimp_id);
                context.SaveChanges();
            }

            return olimp.olimp_id;
        }

        private int SaveNewOlimp(GenioAppEntities context)
        {
            var newOlimp = new Olimp();

            // Заполняем данные мероприятия
            UpdateOlimpData(newOlimp);
            newOlimp.created_date = DateTime.Now;

            // Добавляем и сохраняем мероприятие
            context.Olimps.Add(newOlimp);
            context.SaveChanges();

            // Добавляем участников
            if (studentParticipations.Any())
            {
                AddParticipantsToOlimp(context, newOlimp.olimp_id);
                context.SaveChanges();
            }

            return newOlimp.olimp_id;
        }

        private void UpdateOlimpData(Olimp olimp)
        {
            // Получаем дату
            if (DateTime.TryParse(EventDateTextBox.Text, out DateTime date))
            {
                olimp.olimp_date = date;
            }
            else
            {
                olimp.olimp_date = DateTime.Now;
                EventDateTextBox.Text = olimp.olimp_date.ToString("dd.MM.yyyy");
            }

            olimp.olimp_name = EventNameTextBox.Text.Trim();

            // Получаем уровень
            var levelItem = EventLevelComboBox.SelectedItem as ComboBoxItem;
            if (levelItem != null && !string.IsNullOrWhiteSpace(levelItem.Content.ToString()))
            {
                olimp.olimp_level = levelItem.Content.ToString().Trim();
            }
            else
            {
                olimp.olimp_level = "Город";
            }

            // Получаем тип мероприятия
            var typeItem = EventTypeComboBox.SelectedItem as ComboBoxItem;
            if (typeItem != null && typeItem.Tag != null)
            {
                olimp.event_type_id = (int)typeItem.Tag;
            }
            else
            {
                // Берем первый тип по умолчанию
                olimp.event_type_id = eventTypes.FirstOrDefault()?.event_type_id ?? 1;
            }

            olimp.nominations = (EventNominationsTextBox.Text ?? "").Trim();
            olimp.olimp_location = (EventLocationTextBox.Text ?? "").Trim();
        }

        private void AddParticipantsToOlimp(GenioAppEntities context, int olimpId)
        {
            // Убедимся, что olimpId корректен
            if (olimpId <= 0)
            {
                MessageBox.Show("Неверный ID мероприятия", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Проверяем, существует ли мероприятие
            var olimp = context.Olimps.Find(olimpId);
            if (olimp == null)
            {
                MessageBox.Show("Мероприятие не найдено", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            foreach (var participation in studentParticipations)
            {
                // Проверяем, не добавлен ли уже этот студент к этому мероприятию
                bool alreadyExists = context.StudentOlimps
                    .Any(so => so.olimp_id == olimpId && so.student_id == participation.student_id);

                if (!alreadyExists)
                {
                    // Создаем новый объект StudentOlimp с правильным olimp_id
                    var studentOlimp = new StudentOlimp
                    {
                        student_id = participation.student_id,
                        olimp_id = olimpId,
                        result = participation.result,
                        created_date = participation.created_date ?? DateTime.Now
                    };

                    context.StudentOlimps.Add(studentOlimp);
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (isEditMode && eventId > 0)
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить это мероприятие?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var context = new GenioAppEntities())
                        {
                            // Находим мероприятие
                            var olimp = context.Olimps.Find(eventId);
                            if (olimp != null)
                            {
                                // Участники удаляются автоматически
                                context.Olimps.Remove(olimp);
                                context.SaveChanges();

                                MessageBox.Show("Мероприятие удалено", "Удаление",
                                    MessageBoxButton.OK, MessageBoxImage.Information);

                                // Очищаем временные данные
                                ClearTempData();

                                // Возврат на страницу мероприятий
                                var mainWindow = Window.GetWindow(this) as MainWindow;
                                if (mainWindow != null)
                                {
                                    mainWindow.LoadPage("Olympiads");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void AddStudentButton_Click(object sender, RoutedEventArgs e)
        {
            // Сохраняем данные перед переходом
            SaveTempData();

            // Переход на страницу добавления учащегося с указанием страницы-источника
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                // Передаем информацию о том, что переходим с AddEditOlimpPage
                var page = new AddEditStudPage(false, "AddEditOlimp");
                mainWindow.MainFrame.Navigate(page);
                mainWindow.LockMenu(true);
            }
        }
    }
}