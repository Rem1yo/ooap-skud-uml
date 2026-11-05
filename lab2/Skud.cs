#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Skud;

public class Расписание
{
  private static readonly string[] краткиеДни = { "Вс", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб" };

  public string[] дниНедели { get; set; }
  public string времяНачала { get; set; }
  public string времяОкончания { get; set; }

  public Расписание(string[] дниНедели, string времяНачала, string времяОкончания)
  {
    this.дниНедели = дниНедели;
    this.времяНачала = времяНачала;
    this.времяОкончания = времяОкончания;
  }

  public bool проверитьВремя(DateTime время)
  {
    if (!дниНедели.Contains(краткиеДни[(int)время.DayOfWeek]))
    {
      return false;
    }
    var t = время.TimeOfDay;
    return t >= TimeSpan.Parse(времяНачала) && t <= TimeSpan.Parse(времяОкончания);
  }
}

public class Зона
{
  public string название { get; set; }
  public int уровеньЗащиты { get; set; }
  private readonly List<Расписание> расписания = new();
  public List<Дверь> двери { get; } = new();

  public Зона(string название, int уровеньЗащиты)
  {
    this.название = название;
    this.уровеньЗащиты = уровеньЗащиты;
  }

  public Расписание добавитьРасписание(string[] дниНедели, string времяНачала, string времяОкончания)
  {
    var расписание = new Расписание(дниНедели, времяНачала, времяОкончания);
    расписания.Add(расписание);
    return расписание;
  }

  public bool доступноВВремя(DateTime время)
  {
    return расписания.Count == 0 || расписания.Any(р => р.проверитьВремя(время));
  }
}

public abstract class Считыватель
{
  private readonly string серийныйНомер;

  public string место { get; set; }
  public bool исправен { get; set; } = true;

  protected Считыватель(string серийныйНомер, string место)
  {
    this.серийныйНомер = серийныйНомер;
    this.место = место;
  }

  public abstract string считатьДанные();

  public void показатьРезультат(bool результат)
  {
    Console.WriteLine($"  [{место}] {(результат ? "доступ разрешён" : "доступ запрещён")}");
  }
}

public class КартРидер : Считыватель
{
  private readonly string частота;
  private string сырыеДанные = "";

  public КартРидер(string серийныйНомер, string место, string частота) : base(серийныйНомер, место)
  {
    this.частота = частота;
  }

  public void поднестиКарту(string данныеМетки)
  {
    сырыеДанные = данныеМетки;
  }

  public override string считатьДанные()
  {
    return декодироватьМетку();
  }

  private string декодироватьМетку()
  {
    return сырыеДанные.Trim().ToUpperInvariant();
  }
}

public class БиометрическийСканер : Считыватель
{
  private readonly double порогСовпадения;
  private string образец = "";

  public string тип { get; set; }

  public БиометрическийСканер(string серийныйНомер, string место, string тип, double порогСовпадения) : base(серийныйНомер, место)
  {
    this.тип = тип;
    this.порогСовпадения = порогСовпадения;
  }

  public void предъявитьОбразец(string данныеОбразца)
  {
    образец = данныеОбразца;
  }

  public override string считатьДанные()
  {
    return построитьШаблон();
  }

  private string построитьШаблон()
  {
    var хеш = SHA256.HashData(Encoding.UTF8.GetBytes(образец));
    return Convert.ToHexString(хеш)[..16];
  }
}

public class Дверь
{
  private readonly List<Считыватель> считыватели = new();
  private readonly Зона зона;

  public string номер { get; set; }
  public bool открыта { get; private set; }

  public Дверь(string номер, Зона зона)
  {
    this.номер = номер;
    this.зона = зона;
    зона.двери.Add(this);
  }

  public void добавитьСчитыватель(Считыватель считыватель)
  {
    if (считыватели.Count >= 2)
    {
      throw new InvalidOperationException("У двери не может быть больше двух считывателей");
    }
    считыватели.Add(считыватель);
  }

  public void открыть()
  {
    открыта = true;
  }

  public void закрыть()
  {
    открыта = false;
  }

  public Зона получитьЗону()
  {
    return зона;
  }
}

public class Пропуск
{
  public string номер { get; set; }
  public DateTime датаВыдачи { get; set; }
  public DateTime срокДействия { get; set; }
  public string статус { get; set; } = "активен";
  public List<Зона> зоны { get; } = new();

  public Пропуск(string номер, DateTime датаВыдачи, DateTime срокДействия)
  {
    this.номер = номер;
    this.датаВыдачи = датаВыдачи;
    this.срокДействия = срокДействия;
  }

  public bool действителен()
  {
    return действителен(DateTime.Now);
  }

  public bool действителен(DateTime момент)
  {
    return статус == "активен" && момент >= датаВыдачи && момент <= срокДействия;
  }

  public bool даётДоступ(Зона зона)
  {
    return зоны.Contains(зона);
  }

  public void заблокировать()
  {
    статус = "заблокирован";
  }
}

public class Сотрудник
{
  private readonly string табельныйНомер;

  public string фио { get; set; }
  public string должность { get; set; }
  public List<Пропуск> пропуска { get; } = new();

  public Сотрудник(string табельныйНомер, string фио, string должность)
  {
    this.табельныйНомер = табельныйНомер;
    this.фио = фио;
    this.должность = должность;
  }

  public Пропуск? предъявитьПропуск()
  {
    return пропуска.FirstOrDefault();
  }
}

public class СобытиеПрохода
{
  public DateTime время { get; }
  public string номерПропуска { get; }
  public string номерДвери { get; }
  public bool результат { get; }
  public string? причинаОтказа { get; }

  public СобытиеПрохода(DateTime время, string номерПропуска, string номерДвери, bool результат, string? причинаОтказа)
  {
    this.время = время;
    this.номерПропуска = номерПропуска;
    this.номерДвери = номерДвери;
    this.результат = результат;
    this.причинаОтказа = причинаОтказа;
  }

  public string описать()
  {
    var итог = результат ? "разрешён" : $"отказ ({причинаОтказа})";
    return $"{время:dd.MM.yyyy HH:mm} пропуск {номерПропуска}, дверь {номерДвери}: {итог}";
  }
}

public class ЖурналПроходов
{
  private readonly List<СобытиеПрохода> записи = new();

  public DateTime датаНачала { get; set; }

  public ЖурналПроходов(DateTime датаНачала)
  {
    this.датаНачала = датаНачала;
  }

  public void добавитьЗапись(СобытиеПрохода событие)
  {
    записи.Add(событие);
  }

  public List<СобытиеПрохода> найтиПоПропуску(string номер)
  {
    return записи.Where(з => з.номерПропуска == номер).ToList();
  }
}

public class Контроллер
{
  private readonly int номер;
  private readonly List<Дверь> двери = new();
  private readonly ЖурналПроходов журнал;

  public string режим { get; private set; } = "обычный";
  public Func<DateTime> часы { get; set; } = () => DateTime.Now;

  public Контроллер(int номер, ЖурналПроходов журнал)
  {
    this.номер = номер;
    this.журнал = журнал;
  }

  public void подключитьДверь(Дверь дверь)
  {
    двери.Add(дверь);
  }

  public bool запросДоступа(Пропуск пропуск, Дверь дверь)
  {
    var момент = часы();
    var зона = дверь.получитьЗону();
    string? причинаОтказа = null;

    if (режим != "аварийный")
    {
      причинаОтказа = проверитьПропуск(пропуск, зона, момент);
    }

    var разрешено = причинаОтказа == null;
    if (разрешено)
    {
      дверь.открыть();
    }
    записатьВЖурнал(пропуск, дверь, момент, причинаОтказа);
    return разрешено;
  }

  private string? проверитьПропуск(Пропуск пропуск, Зона зона, DateTime момент)
  {
    var годен = пропуск.действителен(момент);
    var естьДоступ = пропуск.даётДоступ(зона);
    var поРасписанию = зона.доступноВВремя(момент);
    string? причина = null;

    if (!годен)
    {
      причина = "пропуск недействителен";
    }
    else if (!естьДоступ)
    {
      причина = "нет доступа в зону";
    }
    else if (!поРасписанию)
    {
      причина = "вне расписания";
    }
    return причина;
  }

  private void записатьВЖурнал(Пропуск пропуск, Дверь дверь, DateTime момент, string? причина)
  {
    var событие = new СобытиеПрохода(момент, пропуск.номер, дверь.номер, причина == null, причина);
    журнал.добавитьЗапись(событие);
  }

  public void переключитьРежим(string режим)
  {
    this.режим = режим;
    if (режим == "аварийный")
    {
      foreach (var дверь in двери)
      {
        дверь.открыть();
      }
    }
    else
    {
      foreach (var дверь in двери)
      {
        дверь.закрыть();
      }
    }
  }
}

public class АварийныйКонтроллер
{
  private readonly List<Контроллер> контроллеры;

  public string тип { get; private set; } = "";
  public bool активен { get; private set; }

  public АварийныйКонтроллер(List<Контроллер> контроллеры)
  {
    this.контроллеры = контроллеры;
  }

  public void объявитьАварию(string тип)
  {
    this.тип = тип;
    активен = true;
    foreach (var контроллер in контроллеры)
    {
      контроллер.переключитьРежим("аварийный");
    }
  }

  public void отменитьАварию()
  {
    активен = false;
    foreach (var контроллер in контроллеры)
    {
      контроллер.переключитьРежим("обычный");
    }
  }
}

public class ПожарнаяСигнализация
{
  private readonly АварийныйКонтроллер аварийныйКонтроллер;

  public bool сработала { get; private set; }

  public ПожарнаяСигнализация(АварийныйКонтроллер аварийныйКонтроллер)
  {
    this.аварийныйКонтроллер = аварийныйКонтроллер;
  }

  public void податьСигнал()
  {
    сработала = true;
    аварийныйКонтроллер.объявитьАварию("пожар");
  }

  public void сбросить()
  {
    сработала = false;
    аварийныйКонтроллер.отменитьАварию();
  }
}

public static class Program
{
  public static void Main()
  {
    var серверная = new Зона("Серверная", 3);
    серверная.добавитьРасписание(new[] { "Пн", "Вт", "Ср", "Чт", "Пт" }, "08:00", "19:00");

    var д12 = new Дверь("Д-12", серверная);
    var р12 = new КартРидер("CR-0012", "снаружи", "13.56 МГц");
    д12.добавитьСчитыватель(р12);

    var журнал = new ЖурналПроходов(new DateTime(2026, 11, 1));
    var к1 = new Контроллер(1, журнал);
    к1.подключитьДверь(д12);

    var иванов = new Сотрудник("Т-0457", "Иванов И. И.", "инженер");
    var пропуск4501 = new Пропуск("4501", new DateTime(2026, 1, 15), new DateTime(2026, 12, 31));
    пропуск4501.зоны.Add(серверная);
    иванов.пропуска.Add(пропуск4501);

    var базаПропусков = new Dictionary<string, Пропуск> { [пропуск4501.номер] = пропуск4501 };

    void пройти(DateTime момент)
    {
      к1.часы = () => момент;
      р12.поднестиКарту(иванов.предъявитьПропуск()?.номер ?? "");
      var пропуск = базаПропусков[р12.считатьДанные()];
      var разрешено = к1.запросДоступа(пропуск, д12);
      р12.показатьРезультат(разрешено);
      д12.закрыть();
    }

    Console.WriteLine("Понедельник, 09:15:");
    пройти(new DateTime(2026, 11, 2, 9, 15, 0));

    Console.WriteLine("Суббота, 10:00:");
    пройти(new DateTime(2026, 11, 7, 10, 0, 0));

    Console.WriteLine("Пожар:");
    var аварийный = new АварийныйКонтроллер(new List<Контроллер> { к1 });
    var сигнализация = new ПожарнаяСигнализация(аварийный);
    сигнализация.податьСигнал();
    Console.WriteLine($"  режим: {к1.режим}, дверь открыта: {д12.открыта}");
    сигнализация.сбросить();
    Console.WriteLine($"  режим: {к1.режим}, дверь открыта: {д12.открыта}");

    Console.WriteLine("Журнал по пропуску 4501:");
    foreach (var запись in журнал.найтиПоПропуску("4501"))
    {
      Console.WriteLine("  " + запись.описать());
    }
  }
}
