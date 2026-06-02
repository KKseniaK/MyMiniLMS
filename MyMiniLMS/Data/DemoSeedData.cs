namespace MyMiniLMS.Data;

public static class DemoSeedData
{
    public const string AdminPassword = "Admin123!";
    public const string UserPassword = "Password123!";

    public static readonly DemoUser Admin = new(
        "admin@myminilms.com",
        "Администратор системы",
        "Admin",
        AdminPassword);

    public static readonly DemoUser[] Users =
    [
        new("teacher1@myminilms.com", "Иван Петров", "Teacher", UserPassword),
        new("teacher2@myminilms.com", "Елена Соколова", "Teacher", UserPassword),
        new("student1@myminilms.com", "Анна Иванова", "Student", UserPassword),
        new("student2@myminilms.com", "Петр Сидоров", "Student", UserPassword),
        new("student3@myminilms.com", "Мария Смирнова", "Student", UserPassword)
    ];

    public static readonly DemoCourse[] Courses =
    [
        new("Тестирование ПО", "Основы ручного и автоматизированного тестирования.", "teacher1@myminilms.com", "Software Testing"),
        new("Базы данных", "Реляционные базы данных, SQL и проектирование схем.", "teacher1@myminilms.com", "Databases"),
        new("Машинное обучение", "Введение в обучение с учителем и без учителя.", "teacher2@myminilms.com", "Machine Learning"),
        new("Веб-разработка", "ASP.NET Core MVC, HTML, CSS и серверный рендеринг.", "teacher2@myminilms.com", "Web Development"),
        new("Алгоритмы", "Базовые алгоритмы, структуры данных и анализ сложности.", "teacher2@myminilms.com", "Algorithms")
    ];

    public static readonly DemoAssignment[] Assignments =
    [
        new("Тестирование ПО", "Лабораторная 1: Тест-кейсы", "Составить тест-кейсы для простой веб-формы.", 5, "Lab 1: Test Cases"),
        new("Тестирование ПО", "Лабораторная 2: Баг-репорт", "Оформить баг-репорт по стандартному шаблону.", 10, "Lab 2: Bug Report"),
        new("Тестирование ПО", "Лабораторная 3: Чек-лист", "Подготовить чек-лист для проверки формы регистрации.", 14),
        new("Тестирование ПО", "Лабораторная 4: Тест-план", "Описать тест-план для небольшого MVC-приложения.", 18),
        new("Тестирование ПО", "Лабораторная 5: Регрессионное тестирование", "Составить набор регрессионных проверок после исправления дефекта.", 22),

        new("Базы данных", "ER-диаграмма", "Спроектировать ER-диаграмму для мини-LMS.", 7, "ER Diagram"),
        new("Базы данных", "Нормализация", "Привести таблицы к третьей нормальной форме.", 11),
        new("Базы данных", "SQL-запросы", "Написать запросы SELECT, JOIN, GROUP BY и HAVING.", 15),
        new("Базы данных", "Индексы", "Объяснить и протестировать влияние индексов на выборку.", 19),
        new("Базы данных", "Транзакции", "Разобрать пример транзакции и уровней изоляции.", 23),

        new("Машинное обучение", "Метрики классификации", "Посчитать accuracy, precision, recall и ROC-AUC.", 4, "Classification Metrics"),
        new("Машинное обучение", "Random Forest", "Обучить и оценить модель случайного леса.", 12),
        new("Машинное обучение", "Линейная регрессия", "Построить простую модель линейной регрессии.", 16),
        new("Машинное обучение", "Кросс-валидация", "Сравнить качество модели на разных разбиениях данных.", 20),
        new("Машинное обучение", "Подготовка признаков", "Выполнить масштабирование и кодирование категориальных признаков.", 24),

        new("Веб-разработка", "MVC CRUD", "Реализовать CRUD-операции в ASP.NET Core MVC.", 8),
        new("Веб-разработка", "Razor Views", "Сверстать представления для списка и деталей сущности.", 13),
        new("Веб-разработка", "Валидация форм", "Добавить серверную и клиентскую валидацию формы.", 17),
        new("Веб-разработка", "Авторизация", "Ограничить доступ к действиям по ролям пользователей.", 21),
        new("Веб-разработка", "Локализация интерфейса", "Добавить переключение языка для элементов интерфейса.", 25),

        new("Алгоритмы", "Алгоритмы сортировки", "Сравнить bubble sort, quicksort и mergesort.", 6, "Sorting Algorithms"),
        new("Алгоритмы", "Бинарный поиск", "Реализовать бинарный поиск и описать его сложность.", 9),
        new("Алгоритмы", "Стек и очередь", "Решить задачи с использованием стека и очереди.", 13),
        new("Алгоритмы", "Графы", "Реализовать обход графа в глубину и ширину.", 18),
        new("Алгоритмы", "Динамическое программирование", "Решить задачу на динамическое программирование.", 26)
    ];

    public static readonly DemoStudentCourse[] StudentCourses =
    [
        new("student1@myminilms.com", "Тестирование ПО"),
        new("student1@myminilms.com", "Базы данных"),
        new("student2@myminilms.com", "Машинное обучение"),
        new("student2@myminilms.com", "Веб-разработка"),
        new("student3@myminilms.com", "Алгоритмы")
    ];
}

public record DemoUser(string Email, string FullName, string Role, string Password);

public record DemoCourse(string Name, string Description, string TeacherEmail, string? LegacyName = null);

public record DemoAssignment(
    string CourseName,
    string Title,
    string Description,
    int DeadlineDaysFromNow,
    string? LegacyTitle = null);

public record DemoStudentCourse(string StudentEmail, string CourseName);
