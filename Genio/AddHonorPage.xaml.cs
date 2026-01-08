using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Genio
{
    public partial class AddHonorPage : Page
    {
        private bool isEditMode = false;
        private int honorId = 0;
        private List<Student> allStudents = new List<Student>();
        private List<Student> selectedStudents = new List<Student>();
        private List<Specialization> allSpecializations = new List<Specialization>();
        private bool isFormEnabled = false;
        private List<HonorBoard> existingHonors = new List<HonorBoard>();

        public AddHonorPage()
        {
            InitializeComponent();
            Loaded += AddHonorPage_Loaded;
        }

        // Конструктор для режима редактирования
        public AddHonorPage(int honorId) : this()
        {
            this.honorId = honorId;
            this.isEditMode = true;
        }

        private void AddHonorPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Устанавливаем текущую дату
            PlacementDateTextBox.Text = DateTime.Now.ToString("dd.MM.yyyy");

            // Загружаем данные
            LoadDataFromDatabase();

            // Настраиваем режим
            if (isEditMode)
            {
                DeleteButton.Visibility = Visibility.Visible;
                LoadHonorData();
            }

            // Настраиваем обработчики событий
            SetupEventHandlers();

            // Устанавливаем даты по умолчанию для формы
            SetDefaultFormDates();
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
                        .ThenBy(s => s.middle_name)
                        .ToList();

                    // Загружаем специальности
                    allSpecializations = context.Specializations
                        .OrderBy(s => s.spec_name)
                        .ToList();

                    // Заполняем ComboBox для специализации
                    SpecialtyComboBox.Items.Clear();
                    foreach (var spec in allSpecializations)
                    {
                        SpecialtyComboBox.Items.Add(spec.spec_name);
                    }

                    // Загружаем список студентов
                    LoadStudentsList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadHonorData()
        {
            if (honorId <= 0) return;

            try
            {
                using (var context = new GenioAppEntities())
                {
                    // Загружаем существующие записи доски почета для редактирования
                    var honor = context.HonorBoards
                        .Include("Student")
                        .FirstOrDefault(h => h.honor_id == honorId);

                    if (honor != null)
                    {
                        // Устанавливаем дату из выбранной записи
                        PlacementDateTextBox.Text = honor.placement_date.ToString("dd.MM.yyyy");

                        // Загружаем ВСЕ записи доски почета за выбранную дату
                        existingHonors = context.HonorBoards
                            .Include("Student")
                            .Where(h => h.placement_date == honor.placement_date)
                            .ToList();

                        // Добавляем студентов из существующих записей в выбранные
                        foreach (var existingHonor in existingHonors)
                        {
                            if (!selectedStudents.Any(s => s.student_id == existingHonor.Student.student_id))
                            {
                                selectedStudents.Add(existingHonor.Student);
                            }
                        }

                        // Обновляем отображение карточек студентов
                        foreach (var student in selectedStudents)
                        {
                            UpdateStudentCard(student);
                        }

                        // Если у нас есть данные студента из выбранной записи, показываем их в форме
                        if (honor.Student != null)
                        {
                            FillFormWithStudentData(honor.Student);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadStudentsList()
        {
            StudentsList.Children.Clear();

            if (allStudents.Count == 0)
            {
                ShowNoResultsMessage("Список студентов пуст");
                return;
            }

            foreach (var student in allStudents)
            {
                CreateStudentCard(student);
            }
        }

        private void CreateStudentCard(Student student)
        {
            // Создаем Border для карточки студента
            var border = new Border();

            // Проверяем, выбран ли уже этот студент
            bool isSelected = selectedStudents.Any(s => s.student_id == student.student_id);

            // Устанавливаем стиль в зависимости от состояния
            if (isSelected)
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
                Text = $"{student.last_name} {student.first_name} {student.middle_name} • {student.group_name} • {GetCourseName(student.course_number)} курс",
                Foreground = (SolidColorBrush)FindResource("LightTextColor"),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 10, 0),
                TextWrapping = TextWrapping.Wrap
            };

            // Кнопка добавления/удаления
            var button = new Button
            {
                Style = (Style)FindResource("StudentSelectionButtonStyle"),
                Width = 27,
                Height = 27,
                Tag = student,
                Margin = new Thickness(0, 0, 5, 0),
                Cursor = Cursors.Hand
            };

            // Иконка кнопки
            var image = new Image
            {
                Stretch = Stretch.Uniform,
                Width = 15,
                Height = 15
            };

            // Устанавливаем иконку в зависимости от состояния
            if (isSelected)
            {
                image.Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Images/iconRemove.png"));
                button.ToolTip = "Удалить из выбранных";
            }
            else
            {
                image.Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Images/addIcon.png"));
                button.ToolTip = "Добавить в выбранные";
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

            // Добавляем в список
            StudentsList.Children.Add(border);
        }

        private void StudentSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag is Student student)
            {
                // Проверяем, выбран ли уже этот студент
                var existingStudent = selectedStudents.FirstOrDefault(s => s.student_id == student.student_id);

                if (existingStudent != null)
                {
                    // Удаляем из выбранных
                    selectedStudents.Remove(existingStudent);
                }
                else
                {
                    // Добавляем в выбранные
                    selectedStudents.Add(student);
                }

                // Обновляем отображение карточки студента
                UpdateStudentCard(student);
            }
        }

        private void UpdateStudentCard(Student student)
        {
            foreach (var child in StudentsList.Children)
            {
                if (child is Border border && border.Tag is Student stud && stud.student_id == student.student_id)
                {
                    bool isSelected = selectedStudents.Any(s => s.student_id == student.student_id);

                    // Обновляем стиль
                    if (isSelected)
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
                        var button = grid.Children.OfType<Button>().FirstOrDefault();
                        if (button != null)
                        {
                            var image = button.Content as Image;
                            if (image != null)
                            {
                                if (isSelected)
                                {
                                    image.Source = new System.Windows.Media.Imaging.BitmapImage(
                                        new Uri("pack://application:,,,/Images/iconRemove.png"));
                                    button.ToolTip = "Удалить из выбранных";
                                }
                                else
                                {
                                    image.Source = new System.Windows.Media.Imaging.BitmapImage(
                                        new Uri("pack://application:,,,/Images/addIcon.png"));
                                    button.ToolTip = "Добавить в выбранные";
                                }
                            }
                        }
                    }
                    break;
                }
            }
        }

        private void FillFormWithStudentData(Student student)
        {
            FullNameTextBox.Text = $"{student.last_name} {student.first_name} {student.middle_name}";

            // Устанавливаем курс
            switch (student.course_number)
            {
                case 1:
                    Course1Radio.IsChecked = true;
                    break;
                case 2:
                    Course2Radio.IsChecked = true;
                    break;
                case 3:
                    Course3Radio.IsChecked = true;
                    break;
                case 4:
                    Course4Radio.IsChecked = true;
                    break;
            }

            GroupTextBox.Text = student.group_name;

            // Устанавливаем специальность в ComboBox
            if (student.Specialization != null)
            {
                SpecialtyComboBox.SelectedItem = student.Specialization.spec_name;
            }

            AdmissionDateTextBox.Text = student.admission_date.ToString("dd.MM.yyyy");

            if (student.graduation_date.HasValue)
                GraduationDateTextBox.Text = student.graduation_date.Value.ToString("dd.MM.yyyy");

            BirthDateTextBox.Text = student.birth_date.ToString("dd.MM.yyyy");
            PhoneTextBox.Text = student.phone;
            HomePhoneTextBox.Text = student.home_phone;
        }

        private void SetDefaultFormDates()
        {
            var today = DateTime.Now;
            AdmissionDateTextBox.Text = new DateTime(today.Year, 9, 1).ToString("dd.MM.yyyy");
            GraduationDateTextBox.Text = new DateTime(today.Year + 4, 6, 30).ToString("dd.MM.yyyy");
            BirthDateTextBox.Text = new DateTime(today.Year - 18, 1, 1).ToString("dd.MM.yyyy");
        }

        private void SetupEventHandlers()
        {
            // Обработчики для даты занесения
            PlacementDateButton.Click += PlacementDateButton_Click;
            DatePickerCalendar.SelectedDatesChanged += DatePickerCalendar_SelectedDatesChanged;

            // Обработчики для дат в форме
            AdmissionDateButton.Click += (s, e) => ShowDatePickerForTextBox(AdmissionDateTextBox, AdmissionDateButton);
            GraduationDateButton.Click += (s, e) => ShowDatePickerForTextBox(GraduationDateTextBox, GraduationDateButton);
            BirthDateButton.Click += (s, e) => ShowDatePickerForTextBox(BirthDateTextBox, BirthDateButton);

            // Обработчики для поиска
            StudentSearchTextBox.GotFocus += StudentSearchTextBox_GotFocus;
            StudentSearchTextBox.LostFocus += StudentSearchTextBox_LostFocus;
            StudentSearchTextBox.TextChanged += StudentSearchTextBox_TextChanged;
            StudentSearchTextBox.KeyDown += StudentSearchTextBox_KeyDown;

            // Обработчики для кнопок
            BackButton.Click += BackButton_Click;
            SaveButton.Click += SaveButton_Click;
            DeleteButton.Click += DeleteButton_Click;
            AddStudentButton.Click += AddStudentButton_Click;
            ClearFormButton.Click += ClearFormButton_Click;
            SaveFormButton.Click += SaveFormButton_Click;
        }

        private void AddStudentButton_Click(object sender, RoutedEventArgs e)
        {
            // Включаем форму для добавления нового студента
            isFormEnabled = !isFormEnabled;

            if (isFormEnabled)
            {
                // Включаем все элементы формы
                EnableFormControls(true);
                ClearFormButton.IsEnabled = true;
                SaveFormButton.IsEnabled = true;
                AddStudentButton.ToolTip = "Заблокировать форму";
                ClearForm();
            }
            else
            {
                // Отключаем форму
                EnableFormControls(false);
                ClearFormButton.IsEnabled = false;
                SaveFormButton.IsEnabled = false;
                AddStudentButton.ToolTip = "Разблокировать форму для добавления студента";
            }
        }

        private void EnableFormControls(bool enable)
        {
            FullNameTextBox.IsEnabled = enable;
            Course1Radio.IsEnabled = enable;
            Course2Radio.IsEnabled = enable;
            Course3Radio.IsEnabled = enable;
            Course4Radio.IsEnabled = enable;
            GroupTextBox.IsEnabled = enable;
            SpecialtyComboBox.IsEnabled = enable;
            AdmissionDateButton.IsEnabled = enable;
            GraduationDateButton.IsEnabled = enable;
            BirthDateButton.IsEnabled = enable;
            PhoneTextBox.IsEnabled = enable;
            HomePhoneTextBox.IsEnabled = enable;
        }

        private void ClearFormButton_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            FullNameTextBox.Text = "";
            Course1Radio.IsChecked = true;
            GroupTextBox.Text = "";
            SpecialtyComboBox.SelectedIndex = -1;
            SetDefaultFormDates();
            PhoneTextBox.Text = "";
            HomePhoneTextBox.Text = "";
        }

        private void SaveFormButton_Click(object sender, RoutedEventArgs e)
        {
            // Валидация данных формы
            if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
            {
                MessageBox.Show("Введите ФИО студента", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                FullNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(GroupTextBox.Text))
            {
                MessageBox.Show("Введите группу студента", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                GroupTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(SpecialtyComboBox.Text))
            {
                MessageBox.Show("Выберите специальность студента", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SpecialtyComboBox.Focus();
                return;
            }

            try
            {
                using (var context = new GenioAppEntities())
                {
                    // Разделяем ФИО
                    var nameParts = FullNameTextBox.Text.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    string lastName = nameParts.Length > 0 ? nameParts[0] : "";
                    string firstName = nameParts.Length > 1 ? nameParts[1] : "";
                    string middleName = nameParts.Length > 2 ? nameParts[2] : "";

                    // Получаем курс
                    int course = 1;
                    if (Course1Radio.IsChecked == true) course = 1;
                    else if (Course2Radio.IsChecked == true) course = 2;
                    else if (Course3Radio.IsChecked == true) course = 3;
                    else if (Course4Radio.IsChecked == true) course = 4;

                    // Находим или создаем специализацию
                    var specialization = context.Specializations
                        .FirstOrDefault(s => s.spec_name.ToLower() == SpecialtyComboBox.Text.ToLower());

                    if (specialization == null)
                    {
                        specialization = new Specialization
                        {
                            spec_name = SpecialtyComboBox.Text,
                            created_date = DateTime.Now
                        };
                        context.Specializations.Add(specialization);
                        context.SaveChanges();
                    }

                    // Парсим даты
                    DateTime admissionDate;
                    if (!DateTime.TryParse(AdmissionDateTextBox.Text, out admissionDate))
                        admissionDate = new DateTime(DateTime.Now.Year, 9, 1);

                    DateTime? graduationDate = null;
                    if (DateTime.TryParse(GraduationDateTextBox.Text, out DateTime gradDate))
                        graduationDate = gradDate;

                    DateTime birthDate;
                    if (!DateTime.TryParse(BirthDateTextBox.Text, out birthDate))
                        birthDate = new DateTime(DateTime.Now.Year - 18, 1, 1);

                    // Создаем нового студента
                    var newStudent = new Student
                    {
                        last_name = lastName,
                        first_name = firstName,
                        middle_name = middleName,
                        course_number = course,
                        group_name = GroupTextBox.Text,
                        specialization_id = specialization.specialization_id,
                        admission_date = admissionDate,
                        graduation_date = graduationDate,
                        birth_date = birthDate,
                        phone = PhoneTextBox.Text,
                        home_phone = HomePhoneTextBox.Text,
                        created_date = DateTime.Now
                    };

                    context.Students.Add(newStudent);
                    context.SaveChanges();

                    // Обновляем локальный список студентов
                    allStudents = context.Students
                        .Include("Specialization")
                        .OrderBy(s => s.last_name)
                        .ThenBy(s => s.first_name)
                        .ThenBy(s => s.middle_name)
                        .ToList();

                    // Добавляем в выбранные
                    selectedStudents.Add(newStudent);

                    // Обновляем список студентов
                    LoadStudentsList();

                    // Отключаем форму
                    isFormEnabled = false;
                    EnableFormControls(false);
                    ClearFormButton.IsEnabled = false;
                    SaveFormButton.IsEnabled = false;
                    AddStudentButton.ToolTip = "Разблокировать форму для добавления студента";

                    MessageBox.Show("Новый студент добавлен", "Добавление",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления студента: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PlacementDateButton_Click(object sender, RoutedEventArgs e)
        {
            ShowDatePickerForTextBox(PlacementDateTextBox, PlacementDateButton);
        }

        private void ShowDatePickerForTextBox(TextBox textBox, Button button)
        {
            DateTime initialDate;
            if (!DateTime.TryParse(textBox.Text, out initialDate))
                initialDate = DateTime.Now;

            DatePickerCalendar.SelectedDate = initialDate;
            DatePickerCalendar.DisplayDate = initialDate;
            DatePickerPopup.PlacementTarget = button;
            DatePickerPopup.IsOpen = true;
        }

        private void DatePickerCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatePickerCalendar.SelectedDate.HasValue && DatePickerPopup.PlacementTarget is Button button)
            {
                var selectedDate = DatePickerCalendar.SelectedDate.Value;

                // Определяем, для какого TextBox открыт DatePicker
                if (button == PlacementDateButton)
                {
                    PlacementDateTextBox.Text = selectedDate.ToString("dd.MM.yyyy");
                }
                else if (button == AdmissionDateButton)
                {
                    AdmissionDateTextBox.Text = selectedDate.ToString("dd.MM.yyyy");
                }
                else if (button == GraduationDateButton)
                {
                    GraduationDateTextBox.Text = selectedDate.ToString("dd.MM.yyyy");
                }
                else if (button == BirthDateButton)
                {
                    BirthDateTextBox.Text = selectedDate.ToString("dd.MM.yyyy");
                }

                DatePickerPopup.IsOpen = false;
            }
        }

        // МЕТОДЫ ДЛЯ РАБОТЫ С ПОИСКОМ
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
        }

        private void StudentSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (StudentSearchTextBox.Text != "Поиск..." && !string.IsNullOrWhiteSpace(StudentSearchTextBox.Text))
            {
                FilterStudents(StudentSearchTextBox.Text);
            }
            else if (string.IsNullOrWhiteSpace(StudentSearchTextBox.Text) ||
                     StudentSearchTextBox.Text == "Поиск...")
            {
                // Восстанавливаем полный список студентов
                LoadStudentsList();
            }
        }

        private void StudentSearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (StudentSearchTextBox.Text != "Поиск..." && !string.IsNullOrWhiteSpace(StudentSearchTextBox.Text))
                {
                    FilterStudents(StudentSearchTextBox.Text);
                }
                // Убираем фокус с TextBox после нажатия Enter
                Keyboard.ClearFocus();
            }
        }

        private void FilterStudents(string searchText)
        {
            StudentsList.Children.Clear();

            // Приводим поисковый запрос к нижнему регистру для регистронезависимого поиска
            string searchLower = searchText.ToLower();

            // Фильтруем студентов по всем полям
            var filteredStudents = allStudents.Where(s =>
                // Поиск по ФИО (фамилия, имя, отчество отдельно)
                s.last_name.ToLower().Contains(searchLower) ||
                s.first_name.ToLower().Contains(searchLower) ||
                s.middle_name.ToLower().Contains(searchLower) ||

                // Поиск по полному ФИО (через пробел)
                $"{s.last_name} {s.first_name} {s.middle_name}".ToLower().Contains(searchLower) ||

                // Поиск по группе
                s.group_name.ToLower().Contains(searchLower) ||

                // Поиск по курсу (цифра или слово)
                s.course_number.ToString().Contains(searchText) || // для цифр
                GetCourseName(s.course_number).ToLower().Contains(searchLower) || // для слов

                // Поиск по специальности
                (s.Specialization != null && s.Specialization.spec_name.ToLower().Contains(searchLower)) ||

                // Поиск по телефону
                (!string.IsNullOrEmpty(s.phone) && s.phone.Contains(searchText)) ||

                // Поиск по домашнему телефону
                (!string.IsNullOrEmpty(s.home_phone) && s.home_phone.Contains(searchText))
            ).ToList();

            // Сортируем результаты поиска
            var sortedStudents = filteredStudents
                .OrderBy(s => s.last_name)
                .ThenBy(s => s.first_name)
                .ThenBy(s => s.middle_name)
                .ToList();

            // Отображаем отфильтрованных студентов
            foreach (var student in sortedStudents)
            {
                CreateStudentCard(student);
            }

            // Если ничего не найдено, показываем сообщение
            if (sortedStudents.Count == 0)
            {
                ShowNoResultsMessage(searchText);
            }
        }

        private string GetCourseName(int courseNumber)
        {
            switch (courseNumber)
            {
                case 1: return "первый";
                case 2: return "второй";
                case 3: return "третий";
                case 4: return "четвертый";
                default: return "";
            }
        }

        private void ShowNoResultsMessage(string searchText)
        {
            var messageBorder = new Border
            {
                Style = (Style)FindResource("UnselectedStudentItemStyle"),
                Margin = new Thickness(0, 10, 0, 0),
                Height = 50
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var textBlock = new TextBlock
            {
                Text = string.IsNullOrEmpty(searchText) ?
                       "Список студентов пуст" :
                       $"По запросу \"{searchText}\" ничего не найдено",
                Foreground = (SolidColorBrush)FindResource("LightTextColor"),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10),
                TextAlignment = TextAlignment.Center,
                FontStyle = FontStyles.Italic
            };

            Grid.SetColumn(textBlock, 0);
            grid.Children.Add(textBlock);
            messageBorder.Child = grid;

            StudentsList.Children.Add(messageBorder);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Возврат на страницу доски почета
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.LoadPage("HonorBoard");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Валидация данных
            if (string.IsNullOrWhiteSpace(PlacementDateTextBox.Text))
            {
                MessageBox.Show("Выберите дату занесения", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (selectedStudents.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы одного студента", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new GenioAppEntities())
                {
                    if (isEditMode && honorId > 0)
                    {
                        // РЕЖИМ РЕДАКТИРОВАНИЯ - редактируем всю доску почета за выбранную дату
                        if (DateTime.TryParse(PlacementDateTextBox.Text, out DateTime placementDate))
                        {
                            // Удаляем старые записи для этой даты
                            var oldHonors = context.HonorBoards
                                .Where(h => h.placement_date == placementDate)
                                .ToList();

                            context.HonorBoards.RemoveRange(oldHonors);

                            // Создаем новые записи для каждого выбранного студента
                            foreach (var student in selectedStudents)
                            {
                                var honor = new HonorBoard
                                {
                                    student_id = student.student_id,
                                    placement_date = placementDate,
                                    created_date = DateTime.Now
                                };
                                context.HonorBoards.Add(honor);
                            }

                            context.SaveChanges();

                            MessageBox.Show($"Обновлено {selectedStudents.Count} записей на доске почета", "Обновление",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        // РЕЖИМ ДОБАВЛЕНИЯ - создаем записи для каждого выбранного студента
                        if (DateTime.TryParse(PlacementDateTextBox.Text, out DateTime placementDate))
                        {
                            foreach (var student in selectedStudents)
                            {
                                var honor = new HonorBoard
                                {
                                    student_id = student.student_id,
                                    placement_date = placementDate,
                                    created_date = DateTime.Now
                                };
                                context.HonorBoards.Add(honor);
                            }

                            context.SaveChanges();

                            MessageBox.Show($"Добавлено {selectedStudents.Count} записей на доску почета", "Добавление",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }

                    // Возврат на страницу доски почета
                    BackButton_Click(sender, e);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isEditMode || honorId <= 0) return;

            var result = MessageBox.Show("Вы уверены, что хотите удалить ВСЕ записи доски почета за эту дату?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new GenioAppEntities())
                    {
                        // Находим дату из выбранной записи
                        var honor = context.HonorBoards.Find(honorId);
                        if (honor != null)
                        {
                            // Удаляем ВСЕ записи за эту дату
                            var honorsToDelete = context.HonorBoards
                                .Where(h => h.placement_date == honor.placement_date)
                                .ToList();

                            context.HonorBoards.RemoveRange(honorsToDelete);
                            context.SaveChanges();

                            MessageBox.Show($"Удалено {honorsToDelete.Count} записей с доски почета", "Удаление",
                                MessageBoxButton.OK, MessageBoxImage.Information);

                            // Возврат на страницу доски почета
                            BackButton_Click(sender, e);
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
}