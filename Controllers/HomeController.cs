using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IAMCP_Demo_MVC.Controllers
{
    [Authorize]
    [Produces("application/json")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly GraphServiceClient _graphServiceClient;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, GraphServiceClient graphServiceClient)
        {
            _logger = logger;
            _graphServiceClient = graphServiceClient;
        }

        public async Task<IActionResult> Index()
        {
            var events = await _graphServiceClient.Me.Events.Request().GetAsync();

            ViewBag.appointments = events.Select(x => new DefaultSchedule
            {
                Subject = x.Subject,
                StartTime = DateTime.Parse(x.Start.DateTime).ToLocalTime(),
                EndTime = DateTime.Parse(x.End.DateTime).ToLocalTime()
            }).ToList();

            return View();
        }

        [HttpPost]
        public async Task<List<DefaultSchedule>> UpsertData([FromBody] DefaultSchedule param)
        {
            var startDateTimeTimeZone = DateTimeTimeZone.FromDateTime(param.StartTime);
            var endDateTimeTimeZone = DateTimeTimeZone.FromDateTime(param.EndTime);

            var store = await _graphServiceClient.Me.Events.Request().AddAsync(new Event
            {
                Subject = param.Subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = param.Description
                },
                Start = new DateTimeTimeZone
                {
                    DateTime = startDateTimeTimeZone.DateTime,
                    TimeZone = startDateTimeTimeZone.TimeZone
                },
                End = new DateTimeTimeZone
                {
                    DateTime = endDateTimeTimeZone.DateTime,
                    TimeZone = endDateTimeTimeZone.TimeZone
                },
                Attendees = new List<Attendee>()
                {
                    new Attendee
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = User.Claims.FirstOrDefault(x => x.Type == "preferred_username")?.Value,
                            Name = User.Claims.FirstOrDefault(x => x.Type == "name")?.Value
                        },
                        Type = AttendeeType.Required
                    }
                }
            });

            var events = await _graphServiceClient.Me.Events.Request().GetAsync();

            return events.Select(x => new DefaultSchedule { Subject = x.Subject, StartTime = DateTime.Parse(x.Start.DateTime), EndTime = DateTime.Parse(x.End.DateTime) }).ToList();
        }

        public class DefaultSchedule
        {
            public int Id { get; set; }
            public string Subject { get; set; }
            public bool AllDay { get; set; }
            public bool Recurrence { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public string Description { get; set; }
            public string RecurrenceRule { get; set; }
            public string RoomId { get; set; }
            public string OwnerId { get; set; }
        }
    }
}
