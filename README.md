# OWLS - Online Web Library System (DIHS)
ASP.NET Core MVC (.NET 8) + EF Core + SQL Server LocalDB (Server Object Explorer)

## Run in Visual Studio 2022
1. Open OWLS.csproj (File > Open > Project/Solution) and let NuGet restore.
2. Press F5. The database OWLS_DB is created automatically on first run
   (view it in View > SQL Server Object Explorer > (localdb)\MSSQLLocalDB > Databases).
3. Login as admin: admin@dihs.edu.ph / Admin@123
   Register a borrower account from the Register page.

## Using Migrations instead of EnsureCreated (optional)
Remove `db.Database.EnsureCreated();` in Data/DbSeeder.cs and use `db.Database.Migrate();`, then in
Package Manager Console: Add-Migration InitialCreate, then Update-Database.

## Settings
Loan days and penalty per day: Models/BorrowTransaction.cs (LibrarySettings).

## Hi Guys si Natoy to na Mahal na Mahal Kayo
Install niyo muna to (If hindi pa kayo nakakainstall):
	- Microsoft.EntityFrameworkCore.SqlServer
	- Microsoft.EntityFrameworkCore.Tools

Tools > NuGet Package Manager > Package Manager Console > Update-Database