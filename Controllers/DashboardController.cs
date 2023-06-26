using GelirGiderProgramıASPNETCORE.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Syncfusion.EJ2.Charts;
using System.Globalization;

namespace GelirGiderProgramıASPNETCORE.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<ActionResult> Index()
        {
            //Last 7 days

            DateTime StartDate = DateTime.Today.AddDays(-6);
            DateTime EndDate = DateTime.Today;
            //Son 7 günki transactionları cağırıp listeye alıyorum
            List<Transaction> SelectedTransactions = await _context.Transactions.Include(x=>x.Category).Where(y=>y.Date>= StartDate && y.Date<= EndDate).ToListAsync();

            //Toplam gelir gideri hesaplatıyorum
            int TotalIncome = SelectedTransactions.Where(i => i.Category.Type == "Income").Sum(j => j.Amount);
            
            int TotalExpense = SelectedTransactions.Where(i => i.Category.Type == "Expense").Sum(j => j.Amount);

            //Viewbaga Gelir ve gideri atıyorum currency tarzında
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            culture.NumberFormat.CurrencyNegativePattern = 1;
            ViewBag.TotalIncome = String.Format(culture, "{0:C0}", TotalIncome);
            ViewBag.TotalExpense = String.Format(culture, "{0:C0}", TotalExpense);

            int Balance = TotalIncome - TotalExpense;
            ViewBag.Balance = String.Format(culture, "{0:C0}", Balance);

            //donut chart
            ViewBag.DoughnutChartData = SelectedTransactions.Where(i => i.Category.Type == "Expense").GroupBy(j => j.Category.CategoryId).Select(k => new
            {
                categoryTitleWithIcon = k.First().Category.Icon+ " " + k.First().Category.Title,
                amount = k.Sum(j=>j.Amount),
                formattedAmount = k.Sum(j=>j.Amount).ToString("C0")
            }).OrderByDescending(l=>l.amount)
                .ToList();

            //Spline chart

            //Income
            List<SplineChartData> IncomeSummary = SelectedTransactions.Where(i => i.Category.Type == "Income").GroupBy(j => j.Date).Select(k => new SplineChartData()
            {
                day = k.First().Date.ToString("dd-MMM"),
                income = k.Sum(l => l.Amount)
            }).ToList();
            //Expense
            List<SplineChartData> ExpenseSummary = SelectedTransactions.Where(i => i.Category.Type == "Expense").GroupBy(j => j.Date).Select(k => new SplineChartData()
            {
                day = k.First().Date.ToString("dd-MMM"),
                expense = k.Sum(l => l.Amount)
            }).ToList();

            //Combine Income & Expense
            string[] last7Days = Enumerable.Range(0, 7).Select(i => StartDate.AddDays(i).ToString("dd-MMM")).ToArray();

            ViewBag.SplineChartData = from day in last7Days
                                      join income in IncomeSummary on day equals income.day into dayIncomeJoined
                                      from income in dayIncomeJoined.DefaultIfEmpty()
                                      join expense in ExpenseSummary on day equals expense.day into expenseJoined
                                      from expense in expenseJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day = day,
                                          income = income == null ? 0 : income.income,
                                          expense = expense == null ? 0 : expense.expense,
                                      };


            //Recent Transactions

            ViewBag.RecentTransactions = await _context.Transactions.Include(i => i.Category).OrderByDescending(j => j.Date).Take(5).ToListAsync();

            return View();
        }
    }

    public class SplineChartData
    {
        public string day;
        public int income;
        public int expense;
    }
}
