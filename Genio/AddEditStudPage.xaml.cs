using System;
using System.Collections.Generic;
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
        private List<StudentItem> allStudents = new List<StudentItem>();
        private List<StudentItem> filteredStudents = new List<StudentItem>();
        private StudentItem selectedStudent = null;
        private bool isPhoneTextChanging = false;
        private bool isHomePhoneTextChanging = false;

        public class StudentItem
        {
            public int Id { get; set; }
            public string FullName { get; set; }
            public string Course { get; set; }
            public string Group { get; set; }
            public string Specialty { get; set; }
            public DateTime AdmissionDate { get; set; }
            public DateTime GraduationDate { get; set; }
            public DateTime BirthDate { get; set; }
            public string Phone { get; set; }
            public string HomePhone { get; set; }
            public Border StudentBorder { get; set; }
        }

        public AddEditStudPage()
        {
            InitializeComponent();
            Loaded += AddEditStudPage_Loaded;
        }

        public AddEditStudPage(bool editMode) : this()
        {
            this.isEditMode = editMode;
        }

        private void AddEditStudPage_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeTestStudents();

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

        private void InitializeTestStudents()
        {
            allStudents = new List<StudentItem>
            {
                new StudentItem { Id = 1, FullName = "Грекул Злата Анатольевна", Course = "1", Group = "Т-291",
                    Specialty = "Информационные системы и технологии", AdmissionDate = new DateTime(2023, 9, 1),
                    GraduationDate = new DateTime(2027, 6, 30), BirthDate = new DateTime(2005, 3, 15),
                    Phone = "+375 (29) 123-45-67", HomePhone = "+375 (17) 234-56-78" },

                new StudentItem { Id = 2, FullName = "Мотович Маргарита Михайловна", Course = "2", Group = "П-294",
                    Specialty = "Программное обеспечение информационных технологий", AdmissionDate = new DateTime(2022, 9, 1),
                    GraduationDate = new DateTime(2026, 6, 30), BirthDate = new DateTime(2004, 7, 22),
                    Phone = "+375 (29) 234-56-78", HomePhone = "+375 (17) 345-67-89" },

                new StudentItem { Id = 3, FullName = "Садковская Валерия Викторовна", Course = "1", Group = "Т-291",
                    Specialty = "Информационные системы и технологии", AdmissionDate = new DateTime(2023, 9, 1),
                    GraduationDate = new DateTime(2027, 6, 30), BirthDate = new DateTime(2005, 11, 5),
                    Phone = "+375 (29) 345-67-89", HomePhone = "+375 (17) 456-78-90" },

                new StudentItem { Id = 4, FullName = "Пикист Маргарита Сергеевна", Course = "3", Group = "Т-295",
                    Specialty = "Экономика и управление", AdmissionDate = new DateTime(2021, 9, 1),
                    GraduationDate = new DateTime(2025, 6, 30), BirthDate = new DateTime(2003, 4, 18),
                    Phone = "+375 (29) 456-78-90", HomePhone = "+375 (17) 567-89-01" },

                new StudentItem { Id = 5, FullName = "Иванов Алексей Иванович", Course = "1", Group = "К-291",
                    Specialty = "Компьютерная безопасность", AdmissionDate = new DateTime(2023, 9, 1),
                    GraduationDate = new DateTime(2027, 6, 30), BirthDate = new DateTime(2005, 8, 30),
                    Phone = "+375 (29) 567-89-01", HomePhone = "+375 (17) 678-90-12" },

                new StudentItem { Id = 6, FullName = "Паку Евгений Андреевич", Course = "4", Group = "Т-492",
                    Specialty = "Автоматизация технологических процессов", AdmissionDate = new DateTime(2020, 9, 1),
                    GraduationDate = new DateTime(2024, 6, 30), BirthDate = new DateTime(2002, 12, 10),
                    Phone = "+375 (29) 678-90-12", HomePhone = "+375 (17) 789-01-23" },

                new StudentItem { Id = 7, FullName = "Марина Ирина Викторовна", Course = "4", Group = "П-492",
                    Specialty = "Психология", AdmissionDate = new DateTime(2020, 9, 1),
                    GraduationDate = new DateTime(2024, 6, 30), BirthDate = new DateTime(2002, 5, 25),
                    Phone = "+375 (29) 789-01-23", HomePhone = "+375 (17) 890-12-34" }
            };

            filteredStudents = new List<StudentItem>(allStudents);
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

            // Загружаем список учащихся для выбора
            RefreshStudentsList();

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

            // Загружаем список учащихся только для просмотра
            RefreshStudentsList();
        }

        private void RefreshStudentsList()
        {
            StudentsList.Children.Clear();

            foreach (var student in filteredStudents)
            {
                CreateStudentItem(student);
            }
        }

        private void CreateStudentItem(StudentItem student)
        {
            // Создаем Border для элемента
            var border = new Border();
            student.StudentBorder = border;

            // Устанавливаем стиль в зависимости от выбора
            if (selectedStudent != null && student.Id == selectedStudent.Id)
            {
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6155F5")); // Фиолетовый
                border.Cursor = Cursors.Hand;
                border.MouseLeftButtonDown += StudentItem_MouseLeftButtonDown;
            }
            else
            {
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF787878")); // Серый
                if (isEditMode)
                {
                    border.Cursor = Cursors.Hand;
                    border.MouseLeftButtonDown += StudentItem_MouseLeftButtonDown;
                }
                else
                {
                    border.Cursor = Cursors.Arrow;
                }
            }

            border.CornerRadius = new CornerRadius(4);
            border.Height = 27;
            border.Margin = new Thickness(0, 0, 0, 5);
            border.Tag = student;

            // Текст с ФИО и группой
            var textBlock = new TextBlock
            {
                Text = $"{student.FullName} • {student.Group}",
                Foreground = Brushes.White,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };

            border.Child = textBlock;

            // Добавляем в список
            StudentsList.Children.Add(border);
        }

        private void StudentItem_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!isEditMode) return;

            var border = sender as Border;
            if (border != null && border.Tag is StudentItem student)
            {
                // Снимаем выделение с предыдущего элемента
                if (selectedStudent != null && selectedStudent.StudentBorder != null)
                {
                    selectedStudent.StudentBorder.Background =
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF787878"));
                }

                // Выделяем новый элемент
                selectedStudent = student;
                student.StudentBorder.Background =
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6155F5"));

                // Загружаем данные учащегося в форму
                LoadStudentData(student);
            }
        }

        private void LoadStudentData(StudentItem student)
        {
            FullNameTextBox.Text = student.FullName;

            // Устанавливаем курс
            switch (student.Course)
            {
                case "1":
                    Course1Radio.IsChecked = true;
                    break;
                case "2":
                    Course2Radio.IsChecked = true;
                    break;
                case "3":
                    Course3Radio.IsChecked = true;
                    break;
                case "4":
                    Course4Radio.IsChecked = true;
                    break;
            }

            GroupTextBox.Text = student.Group;
            SpecialtyTextBox.Text = student.Specialty;
            AdmissionDateTextBox.Text = student.AdmissionDate.ToString("dd.MM.yyyy");
            GraduationDateTextBox.Text = student.GraduationDate.ToString("dd.MM.yyyy");
            BirthDateTextBox.Text = student.BirthDate.ToString("dd.MM.yyyy");
            PhoneTextBox.Text = student.Phone;
            HomePhoneTextBox.Text = student.HomePhone;
        }

        private void SetDefaultDates()
        {
            if (!isEditMode || selectedStudent == null)
            {
                var today = DateTime.Now;
                AdmissionDateTextBox.Text = new DateTime(today.Year, 9, 1).ToString("dd.MM.yyyy");
                GraduationDateTextBox.Text = new DateTime(today.Year + 4, 6, 30).ToString("dd.MM.yyyy");
                BirthDateTextBox.Text = new DateTime(today.Year - 18, 1, 1).ToString("dd.MM.yyyy");
            }
        }

        // DatePicker логика
        private void AdmissionDateButton_Click(object sender, RoutedEventArgs e)
        {
            currentDateTextBox = AdmissionDateTextBox;
            ShowDatePicker(DateTime.Now);
        }

        private void GraduationDateButton_Click(object sender, RoutedEventArgs e)
        {
            currentDateTextBox = GraduationDateTextBox;
            ShowDatePicker(DateTime.Now.AddYears(4));
        }

        private void BirthDateButton_Click(object sender, RoutedEventArgs e)
        {
            currentDateTextBox = BirthDateTextBox;
            ShowDatePicker(DateTime.Now.AddYears(-18));
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
                FilterStudents(SearchTextBox.Text);
            }
        }

        private void FilterStudents(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText) || searchText == "Поиск...")
            {
                filteredStudents = new List<StudentItem>(allStudents);
            }
            else
            {
                filteredStudents = allStudents.FindAll(student =>
                    student.FullName.ToLower().Contains(searchText.ToLower()) ||
                    student.Group.ToLower().Contains(searchText.ToLower()) ||
                    student.Specialty.ToLower().Contains(searchText.ToLower()));
            }

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
            if (digitsOnly.Length > 12) // Максимум 12 цифр для международного номера
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

            // Корректируем позицию курсора с учетом добавленных символов форматирования
            if (cursorPosition <= text.Length)
            {
                int newPosition = cursorPosition;

                // Подсчитываем, сколько форматирующих символов было добавлено/удалено
                int formattingDifference = formatted.Length - text.Length;

                if (formattingDifference != 0)
                {
                    // Корректируем позицию курсора
                    newPosition += formattingDifference;

                    // Ограничиваем позицию курсора длиной строки
                    newPosition = Math.Max(0, Math.Min(formatted.Length, newPosition));
                }

                textBox.SelectionStart = newPosition;
            }
        }

        // Получение выбранного курса
        private string GetSelectedCourse()
        {
            if (Course1Radio.IsChecked == true) return "1";
            if (Course2Radio.IsChecked == true) return "2";
            if (Course3Radio.IsChecked == true) return "3";
            if (Course4Radio.IsChecked == true) return "4";
            return "1"; // По умолчанию
        }

        // Кнопки управления
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Возврат на страницу AddEditOlimpPage
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.LoadPage("AddEditOlimp");
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

            if (string.IsNullOrWhiteSpace(SpecialtyTextBox.Text))
            {
                MessageBox.Show("Введите специальность учащегося", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SpecialtyTextBox.Focus();
                return;
            }

            // TODO: Сохранение данных в БД
            if (isEditMode && selectedStudent != null)
            {
                MessageBox.Show("Изменения сохранены", "Сохранение",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Обновляем данные в списке
                selectedStudent.FullName = FullNameTextBox.Text;
                selectedStudent.Course = GetSelectedCourse();
                selectedStudent.Group = GroupTextBox.Text;
                selectedStudent.Specialty = SpecialtyTextBox.Text;
                selectedStudent.Phone = PhoneTextBox.Text;
                selectedStudent.HomePhone = HomePhoneTextBox.Text;

                RefreshStudentsList();
            }
            else
            {
                MessageBox.Show("Новый учащийся добавлен", "Добавление",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Очищаем форму
                ClearForm();
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
                        // TODO: Удаление из БД
                        allStudents.Remove(selectedStudent);
                        filteredStudents.Remove(selectedStudent);
                        selectedStudent = null;

                        RefreshStudentsList();
                        ClearForm();

                        MessageBox.Show("Учащийся удален", "Удаление",
                            MessageBoxButton.OK, MessageBoxImage.Information);
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
            SpecialtyTextBox.Text = "";
            SetDefaultDates();
            PhoneTextBox.Text = "";
            HomePhoneTextBox.Text = "";
        }
    }
}