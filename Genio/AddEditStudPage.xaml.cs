using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Genio
{
    public partial class AddEditStudPage : Page
    {
        private bool isEditMode = false;
        private int studentId = 0;
        private TextBox currentDateTextBox = null;
        private List<Student> allStudents = new List<Student>();
        private List<Student> filteredStudents = new List<Student>();
        private Student selectedStudent = null;
        private bool isPhoneTextChanging = false;
        private bool isHomePhoneTextChanging = false;
        private List<Specialization> allSpecializations = new List<Specialization>();

        // Флаги для отслеживания, с какой страницы перешли
        private bool cameFromAddEditOlimp = false;
        private bool cameFromStudentsPage = false;

        public AddEditStudPage()
        {
            InitializeComponent();
            Loaded += AddEditStudPage_Loaded;
        }

        public AddEditStudPage(bool editMode) : this()
        {
            this.isEditMode = editMode;
        }

        // Конструктор с указанием страницы-источника
        public AddEditStudPage(bool editMode, string cameFromPage) : this()
        {
            this.isEditMode = editMode;

            // Устанавливаем флаги в зависимости от страницы-источника
            if (cameFromPage == "AddEditOlimp")
            {
                cameFromAddEditOlimp = true;
            }
            else if (cameFromPage == "StudentsPage")
            {
                cameFromStudentsPage = true;
            }
        }
        // Конструктор для редактирования конкретного студента
        public AddEditStudPage(bool editMode, string cameFromPage, int studentId) : this()
        {
            this.isEditMode = editMode;
            this.studentId = studentId;

            // Устанавливаем флаги в зависимости от страницы-источника
            if (cameFromPage == "AddEditOlimp")
            {
                cameFromAddEditOlimp = true;
            }
            else if (cameFromPage == "StudentsPage")
            {
                cameFromStudentsPage = true;
            }
        }
        private void AddEditStudPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSpecializationsFromDatabase();
            LoadStudentsFromDatabase();

            if (isEditMode)
            {
                SetupEditMode();
            }
            else
            {
                SetupAddMode();
            }

            SetDefaultDates();
        }

        private void LoadSpecializationsFromDatabase()
        {
            try
            {
                using (var context = new GenioAppEntities())
                {
                    allSpecializations = context.Specializations
                        .OrderBy(s => s.spec_name)
                        .ToList();

                    // Очищаем и заполняем ComboBox
                    SpecialtyComboBox.Items.Clear();
                    SpecialtyComboBox.Items.Add(""); // Пустой элемент для возможности очистки

                    foreach (var spec in allSpecializations)
                    {
                        SpecialtyComboBox.Items.Add(spec.spec_name);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки специальностей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadStudentsFromDatabase()
        {
            try
            {
                using (var context = new GenioAppEntities())
                {
                    allStudents = context.Students
                        .Include("Specialization")
                        .OrderBy(s => s.last_name)
                        .ThenBy(s => s.first_name)
                        .ToList();

                    filteredStudents = new List<Student>(allStudents);
                    RefreshStudentsList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки студентов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetupEditMode()
        {
            // Показываем шапку для режима редактирования
            EditModeHeader.Visibility = Visibility.Visible;
            EditModeSpacer.Visibility = Visibility.Visible;

            // Изменяем заголовки
            LeftHeaderText.Text = "Учащиеся";
            RightHeaderText.Text = "Редактирование учащегося";

            // Настраиваем кнопки
            ClearDeleteButton.ToolTip = "Удалить учащегося";
            ClearDeleteImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                new Uri("pack://application:,,,/Images/iconRemove.png"));

            // Устанавливаем обработчики для поиска
            SearchTextBox.GotFocus += SearchTextBox_GotFocus;
            SearchTextBox.LostFocus += SearchTextBox_LostFocus;
            SearchTextBox.TextChanged += SearchTextBox_TextChanged;
        }

        private void SetupAddMode()
        {
            // Скрываем шапку для режима редактирования
            EditModeHeader.Visibility = Visibility.Collapsed;
            EditModeSpacer.Visibility = Visibility.Collapsed;

            // Устанавливаем стандартные заголовки
            LeftHeaderText.Text = "Учащиеся";
            RightHeaderText.Text = "Новый учащийся";

            // Настраиваем кнопки
            ClearDeleteButton.ToolTip = "Очистить форму";
            ClearDeleteImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                new Uri("pack://application:,,,/Images/cleanIcon.png"));
        }

        private void RefreshStudentsList()
        {
            StudentsList.Children.Clear();

            foreach (var student in filteredStudents)
            {
                CreateStudentItem(student);
            }
        }

        private void CreateStudentItem(Student student)
        {
            // Создаем Border для элемента
            var border = new Border();

            // Устанавливаем стиль в зависимости от выбора
            if (selectedStudent != null && student.student_id == selectedStudent.student_id)
            {
                border.Background = (SolidColorBrush)FindResource("AccentColor");
            }
            else
            {
                border.Background = (SolidColorBrush)FindResource("CalendarDayHover");
            }

            border.CornerRadius = new CornerRadius(4);
            border.Height = 27;
            border.Margin = new Thickness(0, 0, 0, 5);
            border.Tag = student.student_id;

            if (isEditMode)
            {
                border.Cursor = Cursors.Hand;
                border.MouseLeftButtonDown += StudentItem_MouseLeftButtonDown;
            }
            else
            {
                border.Cursor = Cursors.Arrow;
            }

            // Текст с ФИО и группой
            var textBlock = new TextBlock
            {
                Text = $"{student.last_name} {student.first_name} {student.middle_name} • {student.group_name}",
                Foreground = (SolidColorBrush)FindResource("LightTextColor"),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };

            border.Child = textBlock;

            // Добавляем в список
            StudentsList.Children.Add(border);
        }

        private void StudentItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!isEditMode) return;

            var border = sender as Border;
            if (border != null && border.Tag is int studentId)
            {
                // Находим студента по ID
                var student = allStudents.FirstOrDefault(s => s.student_id == studentId);
                if (student != null)
                {
                    SelectStudent(student);
                }
            }
        }

        private void SelectStudent(Student student)
        {
            // Снимаем выделение с предыдущего элемента
            if (selectedStudent != null)
            {
                RefreshStudentsList();
            }

            // Выделяем новый элемент
            selectedStudent = student;
            RefreshStudentsList();

            // Загружаем данные учащегося в форму
            LoadStudentData(student);
        }

        private void LoadStudentData(Student student)
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
            else
            {
                SpecialtyComboBox.SelectedIndex = -1;
            }

            AdmissionDateTextBox.Text = student.admission_date.ToString("dd.MM.yyyy");

            if (student.graduation_date.HasValue)
                GraduationDateTextBox.Text = student.graduation_date.Value.ToString("dd.MM.yyyy");

            BirthDateTextBox.Text = student.birth_date.ToString("dd.MM.yyyy");
            PhoneTextBox.Text = student.phone;
            HomePhoneTextBox.Text = student.home_phone;
        }

        private void SetDefaultDates()
        {
            if (!isEditMode || selectedStudent == null)
            {
                var today = DateTime.Now;
                AdmissionDateTextBox.Text = new DateTime(today.Year, 9, 1).ToString("dd.MM.yyyy");
                GraduationDateTextBox.Text = new DateTime(today.Year + 4, 6, 30).ToString("dd.MM.yyyy");
                BirthDateTextBox.Text = new DateTime(today.Year - 18, 1, 1).ToString("dd.MM.yyyy");

                // Очищаем ComboBox
                SpecialtyComboBox.SelectedIndex = -1;
            }
        }

        // DatePicker логика
        private void AdmissionDateButton_Click(object sender, RoutedEventArgs e)
        {
            currentDateTextBox = AdmissionDateTextBox;

            DateTime initialDate;
            if (!DateTime.TryParse(AdmissionDateTextBox.Text, out initialDate))
                initialDate = DateTime.Now;

            ShowDatePicker(initialDate);
        }

        private void GraduationDateButton_Click(object sender, RoutedEventArgs e)
        {
            currentDateTextBox = GraduationDateTextBox;

            DateTime initialDate;
            if (!DateTime.TryParse(GraduationDateTextBox.Text, out initialDate))
                initialDate = DateTime.Now.AddYears(4);

            ShowDatePicker(initialDate);
        }

        private void BirthDateButton_Click(object sender, RoutedEventArgs e)
        {
            currentDateTextBox = BirthDateTextBox;

            DateTime initialDate;
            if (!DateTime.TryParse(BirthDateTextBox.Text, out initialDate))
                initialDate = DateTime.Now.AddYears(-18);

            ShowDatePicker(initialDate);
        }

        private void ShowDatePicker(DateTime initialDate)
        {
            DatePickerCalendar.SelectedDate = initialDate;
            DatePickerCalendar.DisplayDate = initialDate;

            if (currentDateTextBox == AdmissionDateTextBox)
            {
                DatePickerPopup.PlacementTarget = AdmissionDateButton;
            }
            else if (currentDateTextBox == GraduationDateTextBox)
            {
                DatePickerPopup.PlacementTarget = GraduationDateButton;
            }
            else if (currentDateTextBox == BirthDateTextBox)
            {
                DatePickerPopup.PlacementTarget = BirthDateButton;
            }

            DatePickerPopup.IsOpen = true;
        }

        private void DatePickerCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatePickerCalendar.SelectedDate.HasValue && currentDateTextBox != null)
            {
                var selectedDate = DatePickerCalendar.SelectedDate.Value;
                currentDateTextBox.Text = selectedDate.ToString("dd.MM.yyyy");
                DatePickerPopup.IsOpen = false;
            }
        }

        // Поиск учащихся
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
            if (isEditMode)
            {
                if (SearchTextBox.Text != "Поиск..." && !string.IsNullOrWhiteSpace(SearchTextBox.Text))
                {
                    FilterStudents(SearchTextBox.Text);
                }
                else if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
                {
                    filteredStudents = new List<Student>(allStudents);
                    RefreshStudentsList();
                }
            }
        }

        private void FilterStudents(string searchText)
        {
            filteredStudents = allStudents.FindAll(student =>
                student.last_name.ToLower().Contains(searchText.ToLower()) ||
                student.first_name.ToLower().Contains(searchText.ToLower()) ||
                student.middle_name.ToLower().Contains(searchText.ToLower()) ||
                student.group_name.ToLower().Contains(searchText.ToLower()) ||
                (student.Specialization != null && student.Specialization.spec_name.ToLower().Contains(searchText.ToLower())));

            RefreshStudentsList();
        }

        // Валидация и форматирование телефонов
        private void PhoneTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры, плюс и дефис
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c) && c != '+' && c != '-' && c != '(' && c != ')' && c != ' ')
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private void HomePhoneTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры, плюс и дефис
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c) && c != '+' && c != '-' && c != '(' && c != ')' && c != ' ')
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private void PhoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isPhoneTextChanging) return;
            isPhoneTextChanging = true;

            FormatPhoneNumber(PhoneTextBox);

            isPhoneTextChanging = false;
        }

        private void HomePhoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isHomePhoneTextChanging) return;
            isHomePhoneTextChanging = true;

            FormatPhoneNumber(HomePhoneTextBox);

            isHomePhoneTextChanging = false;
        }

        private void FormatPhoneNumber(TextBox textBox)
        {
            // Сохраняем позицию курсора
            int cursorPosition = textBox.SelectionStart;

            // Получаем текст и убираем все нецифровые символы, кроме плюса в начале
            string text = textBox.Text;
            string digitsOnly = Regex.Replace(text, @"[^\d+]", "");

            // Если строка начинается с плюса, сохраняем его
            bool hasPlus = digitsOnly.StartsWith("+");
            if (hasPlus)
            {
                digitsOnly = digitsOnly.Substring(1);
            }

            // Ограничиваем количество цифр
            if (digitsOnly.Length > 12)
            {
                digitsOnly = digitsOnly.Substring(0, 12);
            }

            // Форматируем номер
            string formatted = "";

            if (hasPlus)
            {
                formatted = "+";
            }

            if (digitsOnly.Length >= 3)
            {
                formatted += digitsOnly.Substring(0, 3);
                if (digitsOnly.Length > 3)
                {
                    formatted += " (" + digitsOnly.Substring(3, Math.Min(2, digitsOnly.Length - 3));
                }
                if (digitsOnly.Length > 5)
                {
                    formatted += ") " + digitsOnly.Substring(5, Math.Min(3, digitsOnly.Length - 5));
                }
                if (digitsOnly.Length > 8)
                {
                    formatted += "-" + digitsOnly.Substring(8, Math.Min(2, digitsOnly.Length - 8));
                }
                if (digitsOnly.Length > 10)
                {
                    formatted += "-" + digitsOnly.Substring(10, Math.Min(2, digitsOnly.Length - 10));
                }
            }
            else
            {
                formatted = (hasPlus ? "+" : "") + digitsOnly;
            }

            // Обновляем текст
            textBox.Text = formatted;

            // Корректируем позицию курсора
            if (cursorPosition <= text.Length)
            {
                int newPosition = cursorPosition;
                int formattingDifference = formatted.Length - text.Length;

                if (formattingDifference != 0)
                {
                    newPosition += formattingDifference;
                    newPosition = Math.Max(0, Math.Min(formatted.Length, newPosition));
                }

                textBox.SelectionStart = newPosition;
            }
        }

        // Получение выбранного курса
        private int GetSelectedCourse()
        {
            if (Course1Radio.IsChecked == true) return 1;
            if (Course2Radio.IsChecked == true) return 2;
            if (Course3Radio.IsChecked == true) return 3;
            if (Course4Radio.IsChecked == true) return 4;
            return 1;
        }

        // Разделение ФИО на части
        private bool SplitFullName(string fullName, out string lastName, out string firstName, out string middleName)
        {
            lastName = "";
            firstName = "";
            middleName = "";

            var parts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 3)
            {
                lastName = parts[0];
                firstName = parts[1];
                middleName = parts[2];
                return true;
            }
            else if (parts.Length == 2)
            {
                lastName = parts[0];
                firstName = parts[1];
                return true;
            }
            else if (parts.Length == 1)
            {
                lastName = parts[0];
                return true;
            }

            return false;
        }

        // Кнопки управления - ИЗМЕНЕННЫЙ МЕТОД
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Возврат на нужную страницу в зависимости от того, откуда пришли
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                if (cameFromAddEditOlimp)
                {
                    // Возвращаемся на страницу AddEditOlimpPage
                    mainWindow.LoadPage("AddEditOlimp");
                }
                else if (cameFromStudentsPage)
                {
                    // Возвращаемся на страницу StudentsPage
                    mainWindow.LoadPage("Students");
                }
                else
                {
                    // По умолчанию возвращаемся на AddEditOlimpPage
                    mainWindow.LoadPage("AddEditOlimp");
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Валидация данных
            if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
            {
                MessageBox.Show("Введите ФИО учащегося", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                FullNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(GroupTextBox.Text))
            {
                MessageBox.Show("Введите группу учащегося", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                GroupTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(SpecialtyComboBox.Text))
            {
                MessageBox.Show("Выберите специальность учащегося", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SpecialtyComboBox.Focus();
                return;
            }

            try
            {
                using (var context = new GenioAppEntities())
                {
                    if (isEditMode && selectedStudent != null)
                    {
                        // Режим редактирования - обновляем существующего студента
                        var student = context.Students.Find(selectedStudent.student_id);
                        if (student != null)
                        {
                            UpdateStudentData(student);
                            context.SaveChanges();

                            MessageBox.Show("Изменения сохранены", "Сохранение",
                                MessageBoxButton.OK, MessageBoxImage.Information);

                            // Обновляем данные в списке
                            LoadStudentsFromDatabase();
                            SelectStudent(student);
                        }
                    }
                    else
                    {
                        // Режим добавления - создаем нового студента
                        var newStudent = new Student();
                        UpdateStudentData(newStudent);
                        newStudent.created_date = DateTime.Now;

                        context.Students.Add(newStudent);
                        context.SaveChanges();

                        MessageBox.Show("Новый учащийся добавлен", "Добавление",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Очищаем форму и добавляем в список
                        ClearForm();
                        LoadStudentsFromDatabase();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStudentData(Student student)
        {
            // Разделяем ФИО
            string lastName, firstName, middleName;
            if (SplitFullName(FullNameTextBox.Text, out lastName, out firstName, out middleName))
            {
                student.last_name = lastName;
                student.first_name = firstName;
                student.middle_name = middleName;
            }
            else
            {
                student.last_name = FullNameTextBox.Text;
                student.first_name = "";
                student.middle_name = "";
            }

            student.course_number = GetSelectedCourse();
            student.group_name = GroupTextBox.Text;

            // Находим или создаем специализацию
            student.Specialization = GetOrCreateSpecialization(SpecialtyComboBox.Text);

            // Парсим даты
            if (DateTime.TryParse(AdmissionDateTextBox.Text, out DateTime admissionDate))
                student.admission_date = admissionDate;

            if (DateTime.TryParse(GraduationDateTextBox.Text, out DateTime graduationDate))
                student.graduation_date = graduationDate;
            else
                student.graduation_date = null;

            if (DateTime.TryParse(BirthDateTextBox.Text, out DateTime birthDate))
                student.birth_date = birthDate;

            student.phone = PhoneTextBox.Text;
            student.home_phone = HomePhoneTextBox.Text;
        }

        private Specialization GetOrCreateSpecialization(string specName)
        {
            try
            {
                using (var context = new GenioAppEntities())
                {
                    var specialization = context.Specializations
                        .FirstOrDefault(s => s.spec_name.ToLower() == specName.ToLower());

                    if (specialization == null && !string.IsNullOrWhiteSpace(specName))
                    {
                        specialization = new Specialization
                        {
                            spec_name = specName,
                            created_date = DateTime.Now
                        };
                        context.Specializations.Add(specialization);
                        context.SaveChanges();

                        // Добавляем новую специализацию в ComboBox
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            SpecialtyComboBox.Items.Add(specName);
                        });
                    }

                    return specialization;
                }
            }
            catch
            {
                return null;
            }
        }

        private void ClearDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (isEditMode)
            {
                // Режим удаления
                if (selectedStudent != null)
                {
                    var result = MessageBox.Show("Вы уверены, что хотите удалить этого учащегося?",
                        "Подтверждение удаления",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            using (var context = new GenioAppEntities())
                            {
                                // Находим студента
                                var student = context.Students.Find(selectedStudent.student_id);
                                if (student != null)
                                {
                                    // Удаляем связанные записи (участие в мероприятиях, доску почета)
                                    var participations = context.StudentOlimps
                                        .Where(so => so.student_id == student.student_id)
                                        .ToList();
                                    context.StudentOlimps.RemoveRange(participations);

                                    var honors = context.HonorBoards
                                        .Where(h => h.student_id == student.student_id)
                                        .ToList();
                                    context.HonorBoards.RemoveRange(honors);

                                    // Удаляем студента
                                    context.Students.Remove(student);
                                    context.SaveChanges();

                                    // Обновляем списки
                                    selectedStudent = null;
                                    LoadStudentsFromDatabase();
                                    ClearForm();

                                    MessageBox.Show("Учащийся удален", "Удаление",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
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
                else
                {
                    MessageBox.Show("Выберите учащегося для удаления", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                // Режим очистки
                ClearForm();
            }
        }

        private void ClearForm()
        {
            FullNameTextBox.Text = "";
            Course1Radio.IsChecked = true;
            GroupTextBox.Text = "";
            SpecialtyComboBox.SelectedIndex = -1;
            SetDefaultDates();
            PhoneTextBox.Text = "";
            HomePhoneTextBox.Text = "";
        }
    }
}