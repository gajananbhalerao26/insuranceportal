using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using InsuranceClient.Models;
using InsuranceClient.Models.ViewModels;
using System.IO;
using InsuranceClient.Helpers;
using Microsoft.Extensions.Configuration;

namespace InsuranceClient.Controllers
{
    public class HomeController : Controller
    {
        IConfiguration configuration;
        public HomeController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public IActionResult Index()
        {
            return View();
        }
        
        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CustomerViewModel model)
        {
            if (ModelState.IsValid)
            {
                var customerId = Guid.NewGuid();
                StorageHelper storageHelper = new StorageHelper();
                storageHelper.ConnectionString = configuration.GetConnectionString("StorageConnection");
                // save image to azure blob

               var tempFile =  Path.GetTempFileName();
                using (var fs = new FileStream(tempFile, FileMode.Create,FileAccess.Write))
                {

                    await model.Image.CopyToAsync(fs);
                }
                var fileName = Path.GetFileName(model.Image.FileName);
                var tempPath = Path.GetDirectoryName(tempFile);
                var imagePath = Path.Combine(tempPath, string.Concat(customerId, "", fileName));
                System.IO.File.Move(tempFile, imagePath);
                var imageUrl = await storageHelper.UploadCustomerImage("images", imagePath);

                //save data to azure table
                Customer customer = new Customer(customerId.ToString(), model.InsuranceType);
                customer.FullName = model.FullName;
                customer.Email = model.Email;
                customer.Amount = model.Amount;
                customer.premium = model.premium;
                customer.AppDate = model.AppDate;
                customer.EndDate = model.EndDate;
                customer.ImageUrl = imageUrl;

                await storageHelper.InsertCustomerAsync("customers", customer);

                // add confirmation message to azure queue

                 await storageHelper.AddMessageAsync("insurance-request", customer);

                return RedirectToAction("Index");
            }
            else
            {
                return View();
            }
           
        }
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
