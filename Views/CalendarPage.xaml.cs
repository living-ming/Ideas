using IDEAs.Models;
using IDEAs.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace IDEAs.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CalendarPage : Page
    {
        private DataService _dataService;
        // ȫ�ֱ�����ص����ճ�����
        private static Schedule _schedule;

        // �������Ҳ������ʾ�ĵ�ǰ�����µ��¼�����
        public ObservableCollection<Schedule_Event> FilteredEvents { get; set; } = new ObservableCollection<Schedule_Event>();
        public ObservableCollection<string> Schedule_Categories { get; set; } = new ObservableCollection<string>();
        // ��ǰѡ�е�����
        private DateTimeOffset _selectedDate = DateTimeOffset.Now;
        private bool _currentIsImportant = false;

        public CalendarPage()
        {
            this.InitializeComponent();
            _dataService = ((App)Application.Current).DataService;
            PreloadSchedule();
            // ��ҳ��������ʱ��������������
            this.Loaded += CalendarPage_Loaded;
            Calendar.CalendarViewDayItemChanging += MyCalendarView_CalendarViewDayItemChanging;

        }
        private async void PreloadSchedule()
        {
            _schedule = await _dataService.LoadSchedule() ?? new Schedule();

        }
        private void MyCalendarView_CalendarViewDayItemChanging(CalendarView sender, CalendarViewDayItemChangingEventArgs args)
        {

            DateTime currentDate = args.Item.Date.Date;

            bool hasImportantEvent = _schedule.Schedules.Any(ev =>
                ev.IsImportant &&
                ev.StartTime.HasValue && ev.EndTime.HasValue &&
                currentDate >= ev.StartTime.Value.Date &&
                currentDate <= ev.EndTime.Value.Date);


            if (hasImportantEvent)
            {

                string dayString = args.Item.Date.Day.ToString();

                // ���Բ��� TextBlock�����ı��뵱������ƥ��
                TextBlock dayTextBlock = FindDayNumberTextBlock(args.Item, dayString);

                if (dayTextBlock != null)
                {
                    dayTextBlock.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
                    dayTextBlock.FontSize = dayTextBlock.FontSize * 1.5; // ����20%

                }
            }
            else
            {
                args.Item.ClearValue(CalendarViewDayItem.BackgroundProperty);
                args.Item.ClearValue(CalendarViewDayItem.BorderBrushProperty);
                args.Item.ClearValue(CalendarViewDayItem.BorderThicknessProperty);
            }
        }
        private TextBlock FindDayNumberTextBlock(DependencyObject element, string expectedText)
        {
            if (element is TextBlock tb)
            {
                // �Ƚ� TextBlock ���ı���Ԥ���ı�
                if (tb.Text == expectedText)
                {
                    return tb;
                }
            }

            int count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < count; i++)
            {
                var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(element, i);
                var result = FindDayNumberTextBlock(child, expectedText);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }
        /// <summary>
        /// ҳ�����ʱ�����ճ����ݣ�����ʼ����ͼ
        /// </summary>
        private void CalendarPage_Loaded(object sender, RoutedEventArgs e)
        {
            _selectedDate = DateTimeOffset.Now;
            SelectedDateTextBlock.Text = _selectedDate.ToString("yyyy-MM-dd");
            UpdateFilteredEvents(_selectedDate);
            Schedule_Categories = new ObservableCollection<string>(_schedule.Categories);


            // ����ҪĬ��������¼��ؼ����翪ʼʱ��Ĭ�ϵȣ�
            Event_StartDatePicker.Date = _selectedDate.Date;
            Event_EndDatePicker.Date = _selectedDate.Date;


        }

        /// <summary>
        /// �� CalendarView ��ѡ�����ڷ����仯ʱ����
        /// </summary>
        private void Calendar_SelectedDatesChanged(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
        {
            if (sender.SelectedDates.Count > 0)
            {
                ClearEventInput();
                _selectedDate = sender.SelectedDates[0];
                SelectedDateTextBlock.Text = _selectedDate.ToString("yyyy-MM-dd");
                UpdateFilteredEvents(_selectedDate);
                // ͬʱ�������¼��ؼ���Ĭ�ϵĿ�ʼ����ֵֹ
                Event_StartDatePicker.Date = _selectedDate.Date;
                Event_EndDatePicker.Date = _selectedDate.Date;

            }
        }

        private void UpdateFilteredEvents(DateTimeOffset date)
        {
            FilteredEvents.Clear();
            if (_schedule?.Schedules != null)
            {
                var eventsOnDate = _schedule.Schedules.Where(e =>
                    e.StartTime.HasValue && e.EndTime.HasValue &&
                    // date.Date �� [StartTime.Date, EndTime.Date] ��Χ��
                    date.Date >= e.StartTime.Value.Date && date.Date <= e.EndTime.Value.Date
                );

                foreach (var ev in eventsOnDate)
                {
                    FilteredEvents.Add(ev);
                }
            }
        }

        // ��ȷ�ϡ���ť���������������¼�
        private void Event_Confirm_Click(object sender, RoutedEventArgs e)
        {
            // ��� GridView �Ƿ�ѡ����ĳ��
            var selectedEvent = EventsGridView.SelectedItem as Schedule_Event;

            // ��ȡ�Ҳ�����ؼ��е�����
            string title = Event_TitleTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(title))
            {
                // ��ȡ��ʼ���ں�ʱ��
                DateTime startDate = Event_StartDatePicker.Date?.Date ?? DateTime.Now.Date;

                // ��ȡ�������ں�ʱ��
                DateTime endDate = Event_EndDatePicker.Date?.Date ?? startDate;
                // ��ȡ�Ƿ����״̬
                bool isCompleted = Event_isCompleted.IsChecked == true;
                // ��Ҫ��״̬ͨ�� _currentIsImportant ��¼

                if (selectedEvent != null)
                {
                    // ����ѡ�е��¼�
                    selectedEvent.Title = title;
                    selectedEvent.IsCompleted = isCompleted;
                    selectedEvent.IsImportant = _currentIsImportant;
                    selectedEvent.StartTime = startDate;
                    selectedEvent.EndTime = endDate;

                    SaveData();
                }
                else
                {
                    // �������¼�����������״̬
                    var newEvent = new Schedule_Event
                    {
                        Title = title,
                        StartTime = startDate,
                        EndTime = endDate,
                        IsCompleted = isCompleted,
                        IsImportant = _currentIsImportant
                    };


                    _schedule.Schedules.Add(newEvent);
                    if (newEvent.StartTime.HasValue && newEvent.StartTime.Value.Date == _selectedDate.Date)
                    {
                        FilteredEvents.Add(newEvent);
                    }
                    SaveData();
                }

                // �������ؼ������ش�������

                ClearEventInput();
            }
        }

        private void Event_back_Click(object sender, RoutedEventArgs e)
        {
            ClearEventInput();

        }

        // �������ؼ����ɸ���ʵ����Ҫ��װ��
        private void ClearEventInput()
        {
            Event_TitleTextBox.Text = "";
            Event_isCompleted.IsChecked = false;

            // ʹ�õ�ǰѡ�е����� _selectedDate �������ں�ʱ��ؼ�

            Event_StartDatePicker.Date = _selectedDate;
            Event_EndDatePicker.Date = _selectedDate;

            // ������Ҫ��״̬
            _currentIsImportant = false;
            Event_ImportantIcon.Glyph = "\uE734"; // ��ʾ����Ҫͼ��
            EventsGridView.SelectedItem = null; // ���� EventsGridView.SelectedIndex = -1;

        }

        // ����ӡ���ť�������ʾ��
        private void Event_Add_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var flyout = new MenuFlyout();

                // ��ȡ��ǰѡ�е��¼�
                var selectedEvent = EventsGridView.SelectedItem as Schedule_Event;

                // �����ǰû��ѡ���¼�����ֱ�ӷ���
                if (selectedEvent == null)
                {
                    return;
                }

                // ��ӷ���˵���
                foreach (var categoryName in Schedule_Categories)
                {
                    var menuItem = new MenuFlyoutItem
                    {
                        Text = categoryName,
                        DataContext = selectedEvent
                    };
                    menuItem.Click += CategoryMenuItem_Click;
                    flyout.Items.Add(menuItem);
                }

                // ��ӷָ���
                flyout.Items.Add(new MenuFlyoutSeparator());

                // ��ӡ��Ƴ����ࡱ�˵���
                var removeCategoryItem = new MenuFlyoutItem
                {
                    Text = "�Ƴ�����",
                    DataContext = selectedEvent
                };
                removeCategoryItem.Click += RemoveCategoryMenuItem_Click;
                flyout.Items.Add(removeCategoryItem);

                // ��ʾ MenuFlyout
                flyout.ShowAt(button);
            }
        }
        private void CategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuFlyoutItem;
            var ev = menuItem?.DataContext as Schedule_Event;

            if (ev != null)
            {
                ev.Category = menuItem.Text;
                SaveData();
            }
        }
        private void RemoveCategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuFlyoutItem;
            var ev = menuItem?.DataContext as Schedule_Event;

            if (ev != null)
            {
                ev.Category = null;
                SaveData();
            }
        }


        // ��Ҫ���л�ʾ��
        private void Event_IsImportant_Click(object sender, RoutedEventArgs e)
        {
            // �л���Ҫ��״̬��true ��ʾ��Ҫ��false ��ʾ����Ҫ��
            _currentIsImportant = !_currentIsImportant;
            // ���°�ťͼ�꣺���� "\uE734" ��ʾ��Ҫ��"\uE735" ��ʾ����Ҫ
            Event_ImportantIcon.Glyph = _currentIsImportant ? "\uE735" : "\uE734";

        }

        // GridView�е���¼����������ڱ༭��ѡ�������¼�
        private void EventsGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var ev = e.ClickedItem as Schedule_Event;
            if (ev != null)
            {
                // �����������
                Event_TitleTextBox.Text = ev.Title;

                // �����Ƿ����״̬
                Event_isCompleted.IsChecked = ev.IsCompleted;

                // ���ÿ�ʼʱ��
                if (ev.StartTime.HasValue)
                {
                    Event_StartDatePicker.Date = ev.StartTime.Value.Date;
                }

                // ���ý���ʱ��
                if (ev.EndTime.HasValue)
                {
                    Event_EndDatePicker.Date = ev.EndTime.Value.Date;
                }

                // ������Ҫ��״̬������ͼ�겢��¼״̬
                Event_ImportantIcon.Glyph = ev.IsImportantGlyph;
                _currentIsImportant = ev.IsImportant;
            }
        }
        private void DeleteMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // sender �� MenuFlyoutItem��ͨ�� DataContext ��ȡ��Ӧ���¼�����
            var menuItem = sender as MenuFlyoutItem;
            var eventToDelete = menuItem?.DataContext as Schedule_Event;
            if (eventToDelete != null)
            {
                // ��ȫ���ճ̵������б���ɾ��
                _schedule.Schedules.Remove(eventToDelete);

                // ������¼��ڵ�ǰѡ�����ڵ��¼������У���ɾ��֮��ȷ�� UI ͬ�����£�
                if (FilteredEvents.Contains(eventToDelete))
                {
                    FilteredEvents.Remove(eventToDelete);
                }

                // ���ñ��淽���������ݳ־û�
                SaveData();
                ClearEventInput();

            }
        }

        private void SaveData()
        {

            // ���浽�ļ�
            _dataService.SaveToFile(_schedule);

        }

    }
}
