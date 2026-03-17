using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Genio
{
    public partial class SettingsPage : Page
    {
        private List<Specialization> allSpecializations = new List<Specialization>();
        private List<EventType> allEventTypes = new List<EventType>();
        private Specialization currentEditingSpecialization = null;
        private EventType currentEditingEventType = null;

        public SettingsPage()
        {
            InitializeComponent();
            Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSpecializationsFromDatabase();
            LoadEventTypesFromDatabase();
        }

        private void LoadSpecializationsFromDatabase()
        {
            try
            {
                using (var context = new GenioAppEntities())
                {
                    allSpecializations = context.Specializations_GetAll();
                    RefreshSpecialtiesList();
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка загрузки специальностей: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
            }
        }

        private void LoadEventTypesFromDatabase()
        {
            try
            {
                using (var context = new GenioAppEntities())
                {
                    allEventTypes = context.EventTypes_GetAll();
                    RefreshEventTypesList();
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка загрузки видов мероприятий: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
            }
        }

        private void RefreshSpecialtiesList()
        {
            SpecialtiesList.Children.Clear();

            foreach (var specialization in allSpecializations)
            {
                CreateSpecialtyItem(specialization);
            }
        }

        private void RefreshEventTypesList()
        {
            EventTypesList.Children.Clear();

            foreach (var eventType in allEventTypes)
            {
                CreateEventTypeItem(eventType);
            }
        }

        private void CreateSpecialtyItem(Specialization specialization)
        {
            var border = new Border();

            if (currentEditingSpecialization != null && currentEditingSpecialization.specialization_id == specialization.specialization_id)
            {
                border.Style = (Style)FindResource("SelectedStudentItemStyle");
            }
            else
            {
                border.Style = (Style)FindResource("UnselectedStudentItemStyle");
            }

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBox textBox = null;

            if (currentEditingSpecialization != null && currentEditingSpecialization.specialization_id == specialization.specialization_id)
            {
                textBox = new TextBox
                {
                    Text = specialization.spec_name,
                    Style = (Style)FindResource("SearchTextBoxStyle"),
                    Background = Brushes.Transparent,
                    Foreground = (SolidColorBrush)FindResource("LightTextColor"),
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 10, 0),
                    BorderThickness = new Thickness(0)
                };

                Grid.SetColumn(textBox, 0);
                grid.Children.Add(textBox);
            }
            else
            {
                var textBlock = new TextBlock
                {
                    Text = specialization.spec_name,
                    Foreground = (SolidColorBrush)FindResource("LightTextColor"),
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 10, 0)
                };

                Grid.SetColumn(textBlock, 0);
                grid.Children.Add(textBlock);
            }

            var editSaveButton = new Button
            {
                Style = (Style)FindResource("StudentSelectionButtonStyle"),
                Width = 27,
                Height = 27,
                Tag = specialization,
                Margin = new Thickness(5, 0, 5, 0)
            };

            var editSaveImage = new Image
            {
                Stretch = Stretch.Uniform,
                Width = 15,
                Height = 15
            };

            if (currentEditingSpecialization != null && currentEditingSpecialization.specialization_id == specialization.specialization_id)
            {
                editSaveImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Images/saveIcon.png"));
                editSaveButton.ToolTip = "Сохранить изменения";
                editSaveButton.Click += SaveSpecialtyButton_Click;
            }
            else
            {
                editSaveImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Images/editIcon.png"));
                editSaveButton.ToolTip = "Редактировать";
                editSaveButton.Click += EditSpecialtyButton_Click;
            }

            editSaveButton.Content = editSaveImage;
            Grid.SetColumn(editSaveButton, 1);
            grid.Children.Add(editSaveButton);

            var deleteButton = new Button
            {
                Style = (Style)FindResource("DeleteListItemButtonStyle"),
                Width = 27,
                Height = 27,
                Tag = specialization,
                Margin = new Thickness(0, 0, 5, 0)
            };

            var deleteImage = new Image
            {
                Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Images/iconRemove.png")),
                Stretch = Stretch.Uniform,
                Width = 15,
                Height = 15
            };

            deleteButton.Content = deleteImage;
            deleteButton.ToolTip = "Удалить";
            deleteButton.Click += DeleteSpecialtyButton_Click;
            Grid.SetColumn(deleteButton, 2);
            grid.Children.Add(deleteButton);

            border.Child = grid;
            border.Tag = specialization;

            SpecialtiesList.Children.Add(border);
        }

        private void CreateEventTypeItem(EventType eventType)
        {
            var border = new Border();

            if (currentEditingEventType != null && currentEditingEventType.event_type_id == eventType.event_type_id)
            {
                border.Style = (Style)FindResource("SelectedStudentItemStyle");
            }
            else
            {
                border.Style = (Style)FindResource("UnselectedStudentItemStyle");
            }

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBox textBox = null;

            if (currentEditingEventType != null && currentEditingEventType.event_type_id == eventType.event_type_id)
            {
                textBox = new TextBox
                {
                    Text = eventType.type_name,
                    Style = (Style)FindResource("SearchTextBoxStyle"),
                    Background = Brushes.Transparent,
                    Foreground = (SolidColorBrush)FindResource("LightTextColor"),
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 10, 0),
                    BorderThickness = new Thickness(0)
                };

                Grid.SetColumn(textBox, 0);
                grid.Children.Add(textBox);
            }
            else
            {
                var textBlock = new TextBlock
                {
                    Text = eventType.type_name,
                    Foreground = (SolidColorBrush)FindResource("LightTextColor"),
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 10, 0)
                };

                Grid.SetColumn(textBlock, 0);
                grid.Children.Add(textBlock);
            }

            var editSaveButton = new Button
            {
                Style = (Style)FindResource("StudentSelectionButtonStyle"),
                Width = 27,
                Height = 27,
                Tag = eventType,
                Margin = new Thickness(5, 0, 5, 0)
            };

            var editSaveImage = new Image
            {
                Stretch = Stretch.Uniform,
                Width = 15,
                Height = 15
            };

            if (currentEditingEventType != null && currentEditingEventType.event_type_id == eventType.event_type_id)
            {
                editSaveImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Images/saveIcon.png"));
                editSaveButton.ToolTip = "Сохранить изменения";
                editSaveButton.Click += SaveEventTypeButton_Click;
            }
            else
            {
                editSaveImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Images/editIcon.png"));
                editSaveButton.ToolTip = "Редактировать";
                editSaveButton.Click += EditEventTypeButton_Click;
            }

            editSaveButton.Content = editSaveImage;
            Grid.SetColumn(editSaveButton, 1);
            grid.Children.Add(editSaveButton);

            var deleteButton = new Button
            {
                Style = (Style)FindResource("DeleteListItemButtonStyle"),
                Width = 27,
                Height = 27,
                Tag = eventType,
                Margin = new Thickness(0, 0, 5, 0)
            };

            var deleteImage = new Image
            {
                Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Images/iconRemove.png")),
                Stretch = Stretch.Uniform,
                Width = 15,
                Height = 15
            };

            deleteButton.Content = deleteImage;
            deleteButton.ToolTip = "Удалить";
            deleteButton.Click += DeleteEventTypeButton_Click;
            Grid.SetColumn(deleteButton, 2);
            grid.Children.Add(deleteButton);

            border.Child = grid;
            border.Tag = eventType;

            EventTypesList.Children.Add(border);
        }

        private void AddSpecialtyButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SpecialtyTextBox.Text))
            {
                CustomMessageBox.Show("Введите название специальности", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var context = new GenioAppEntities())
                {
                    bool exists = context.Specializations_GetAll()
                        .Any(s => s.spec_name.ToLower().Trim() == SpecialtyTextBox.Text.Trim().ToLower());

                    if (exists)
                    {
                        CustomMessageBox.Show("Такая специальность уже существует!", "Ошибка дублирования",
                            CustomMessageBoxButton.OK, CustomMessageBoxIcon.Warning);
                        SpecialtyTextBox.Focus();
                        SpecialtyTextBox.SelectAll();
                        return;
                    }

                    context.Specializations_Insert(SpecialtyTextBox.Text.Trim());

                    LoadSpecializationsFromDatabase();

                    SpecialtyTextBox.Text = "";
                    SpecialtyTextBox.Focus();

                    CustomMessageBox.Show("Специальность успешно добавлена", "Успешно",
                        CustomMessageBoxButton.OK, CustomMessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка добавления специальности: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
            }
        }

        private void AddEventTypeButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EventTypeTextBox.Text))
            {
                CustomMessageBox.Show("Введите название вида мероприятия", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var context = new GenioAppEntities())
                {
                    bool exists = context.EventTypes_GetAll()
                        .Any(et => et.type_name.ToLower().Trim() == EventTypeTextBox.Text.Trim().ToLower());

                    if (exists)
                    {
                        CustomMessageBox.Show("Такой вид мероприятия уже существует!", "Ошибка",
                            CustomMessageBoxButton.OK, CustomMessageBoxIcon.Warning);
                        return;
                    }

                    context.EventTypes_Insert(EventTypeTextBox.Text.Trim());

                    LoadEventTypesFromDatabase();

                    EventTypeTextBox.Text = "";

                    CustomMessageBox.Show("Вид мероприятия успешно добавлен", "Успешно",
                        CustomMessageBoxButton.OK, CustomMessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Ошибка добавления вида мероприятия: {ex.Message}", "Ошибка",
                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
            }
        }

        private void EditSpecialtyButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag is Specialization specialization)
            {
                currentEditingSpecialization = specialization;
                RefreshSpecialtiesList();
            }
        }

        private void SaveSpecialtyButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag is Specialization specialization && currentEditingSpecialization != null)
            {
                try
                {
                    var border = button.Parent as Grid;
                    if (border != null && border.Children[0] is TextBox textBox)
                    {
                        string newName = textBox.Text.Trim();

                        if (string.IsNullOrWhiteSpace(newName))
                        {
                            CustomMessageBox.Show("Название специальности не может быть пустым", "Ошибка",
                                CustomMessageBoxButton.OK, CustomMessageBoxIcon.Warning);
                            return;
                        }

                        if (newName == specialization.spec_name)
                        {
                            currentEditingSpecialization = null;
                            LoadSpecializationsFromDatabase();
                            return;
                        }

                        using (var context = new GenioAppEntities())
                        {
                            bool exists = context.Specializations_GetAll()
                                .Any(s => s.specialization_id != specialization.specialization_id &&
                                       s.spec_name.ToLower().Trim() == newName.ToLower());

                            if (exists)
                            {
                                CustomMessageBox.Show("Такая специальность уже существует!", "Ошибка дублирования",
                                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Warning);
                                textBox.Focus();
                                textBox.SelectAll();
                                return;
                            }

                            context.Specializations_Update(specialization.specialization_id, newName);

                            currentEditingSpecialization = null;
                            LoadSpecializationsFromDatabase();

                            CustomMessageBox.Show("Специальность успешно обновлена", "Успешно",
                                CustomMessageBoxButton.OK, CustomMessageBoxIcon.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Ошибка обновления специальности: {ex.Message}", "Ошибка",
                        CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
                }
            }
        }

        private void DeleteSpecialtyButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag is Specialization specialization)
            {
                var result = CustomMessageBox.Show($"Вы уверены, что хотите удалить специальность \"{specialization.spec_name}\"?\n\n" +
                    "Внимание: Это действие нельзя отменить.",
                    "Подтверждение удаления",
                    CustomMessageBoxButton.YesNo, CustomMessageBoxIcon.Warning);

                if (result == CustomMessageBoxResult.Yes)
                {
                    try
                    {
                        using (var context = new GenioAppEntities())
                        {
                            bool hasStudents = context.Students_GetAll()
                                .Any(s => s.specialization_id == specialization.specialization_id);

                            if (hasStudents)
                            {
                                var studentCount = context.Students_GetAll()
                                    .Count(s => s.specialization_id == specialization.specialization_id);

                                CustomMessageBox.Show($"Невозможно удалить специальность, так как с ней связано {studentCount} студент(ов).\n\n" +
                                    "Сначала измените специальность у этих студентов или удалите их.", "Ошибка",
                                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
                                return;
                            }

                            context.Specializations_Delete(specialization.specialization_id);

                            if (currentEditingSpecialization != null &&
                                currentEditingSpecialization.specialization_id == specialization.specialization_id)
                            {
                                currentEditingSpecialization = null;
                            }

                            LoadSpecializationsFromDatabase();

                            CustomMessageBox.Show("Специальность успешно удалена", "Успешно",
                                CustomMessageBoxButton.OK, CustomMessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        CustomMessageBox.Show($"Ошибка удаления специальности: {ex.Message}", "Ошибка",
                            CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
                    }
                }
            }
        }

        private void EditEventTypeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag is EventType eventType)
            {
                currentEditingEventType = eventType;
                RefreshEventTypesList();
            }
        }

        private void SaveEventTypeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag is EventType eventType && currentEditingEventType != null)
            {
                try
                {
                    var border = button.Parent as Grid;
                    if (border != null && border.Children[0] is TextBox textBox)
                    {
                        string newName = textBox.Text.Trim();

                        if (string.IsNullOrWhiteSpace(newName))
                        {
                            CustomMessageBox.Show("Название вида мероприятия не может быть пустым", "Ошибка",
                                CustomMessageBoxButton.OK, CustomMessageBoxIcon.Warning);
                            return;
                        }

                        using (var context = new GenioAppEntities())
                        {
                            bool exists = context.EventTypes_GetAll()
                                .Any(et => et.event_type_id != eventType.event_type_id &&
                                       et.type_name.ToLower().Trim() == newName.ToLower());

                            if (exists)
                            {
                                CustomMessageBox.Show("Такой вид мероприятия уже существует!", "Ошибка",
                                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Warning);
                                return;
                            }

                            context.EventTypes_Update(eventType.event_type_id, newName);

                            currentEditingEventType = null;
                            LoadEventTypesFromDatabase();

                            CustomMessageBox.Show("Вид мероприятия успешно обновлен", "Успешно",
                                CustomMessageBoxButton.OK, CustomMessageBoxIcon.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Ошибка обновления вида мероприятия: {ex.Message}", "Ошибка",
                        CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
                }
            }
        }

        private void DeleteEventTypeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag is EventType eventType)
            {
                var result = CustomMessageBox.Show($"Вы уверены, что хотите удалить вид мероприятия \"{eventType.type_name}\"?",
                    "Подтверждение удаления",
                    CustomMessageBoxButton.YesNo, CustomMessageBoxIcon.Warning);

                if (result == CustomMessageBoxResult.Yes)
                {
                    try
                    {
                        using (var context = new GenioAppEntities())
                        {
                            bool hasEvents = context.Olimps_GetAll()
                                .Any(o => o.event_type_id == eventType.event_type_id);

                            if (hasEvents)
                            {
                                var eventCount = context.Olimps_GetAll()
                                    .Count(o => o.event_type_id == eventType.event_type_id);

                                CustomMessageBox.Show($"Невозможно удалить вид мероприятия, так как с ним связано {eventCount} мероприятие(й).\n\n" +
                                    "Сначала измените вид у этих мероприятий.", "Ошибка",
                                    CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
                                return;
                            }

                            context.EventTypes_Delete(eventType.event_type_id);

                            if (currentEditingEventType != null &&
                                currentEditingEventType.event_type_id == eventType.event_type_id)
                            {
                                currentEditingEventType = null;
                            }

                            LoadEventTypesFromDatabase();

                            CustomMessageBox.Show("Вид мероприятия успешно удален", "Успешно",
                                CustomMessageBoxButton.OK, CustomMessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        CustomMessageBox.Show($"Ошибка удаления вида мероприятия: {ex.Message}", "Ошибка",
                            CustomMessageBoxButton.OK, CustomMessageBoxIcon.Error);
                    }
                }
            }
        }

        private void SpecialtyTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddSpecialtyButton_Click(sender, e);
            }
        }

        private void EventTypeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddEventTypeButton_Click(sender, e);
            }
        }
    }
}