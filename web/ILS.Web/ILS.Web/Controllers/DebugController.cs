﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ILS.Domain;
using ILS.Domain.GameAchievements;
using ILS.Models;
using System.Web.Routing;
using System.IO;
using ILS.Web.Rating;

namespace ILS.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DebugController : Controller
    {

        ILSContext context;
		public DebugController(ILSContext context)
		{
			this.context = context;
		}

        public IFormsAuthenticationService FormsService { get; set; }
        public IMembershipService MembershipService { get; set; }

        protected override void Initialize(RequestContext requestContext)
        {
            if (FormsService == null) { FormsService = new FormsAuthenticationService(); }
            if (MembershipService == null) { MembershipService = new AccountMembershipService(); }
            context.Database.Initialize(false);
            base.Initialize(requestContext);
        }

        public JsonResult CreateTestRuns(String name)
        {
            User u = null;
            if (name != null)
            {
                u = context.User.FirstOrDefault(x => x.Name == name);
                if (u == null)
                    return Json(new
                    {
                        success = false,
                        errorMessage = "User with name " + name + " not found"
                    }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                bool ifGuest = !HttpContext.User.Identity.IsAuthenticated;
                if (!ifGuest) u = context.User.FirstOrDefault(x => x.Name == HttpContext.User.Identity.Name);
                if (u == null)
                    return Json(new
                    {
                        success = false,
                        errorMessage = "Authenticated user not found not found"
                    }, JsonRequestBehavior.AllowGet);
            }
            return DoCreateTestRuns(u);
        }

        private JsonResult DoCreateTestRuns(User u)
        {
            Course course = context.Course.First();
            if (course == null)
            {
                return Json(new
                {
                    success = false,
                    errorMessage = "No courses in DB"
                }, JsonRequestBehavior.AllowGet);
            }
            Theme theme = context.Theme.FirstOrDefault(x => x.Course_Id.Equals(course.Id));
            if (theme == null)
            {
                return Json(new
                {
                    success = false,
                    errorMessage = "No themes in course " + course.Name
                }, JsonRequestBehavior.AllowGet);
            }
            Lecture lecture = (Lecture)context.ThemeContent.FirstOrDefault(x => x.Theme_Id.Equals(theme.Id) && x is Lecture);
            if (lecture == null)
            {
                return Json(new
                {
                    success = false,
                    errorMessage = "No lectures in theme " + theme.Name
                }, JsonRequestBehavior.AllowGet);
            }
            Test test = (Test)context.ThemeContent.FirstOrDefault(x => x.Theme_Id.Equals(theme.Id) && x is Test);
            if (test == null)
            {
                return Json(new
                {
                    success = false,
                    errorMessage = "No tests in theme " + theme.Name
                }, JsonRequestBehavior.AllowGet);
            }
            Paragraph paragraph = context.Paragraph.First(x => x.Lecture_Id.Equals(lecture.Id));
            if (paragraph == null)
            {
                return Json(new
                {
                    success = false,
                    errorMessage = "No paragraphs in lecture " + lecture.Name
                }, JsonRequestBehavior.AllowGet);
            }
            Question question = context.Question.First(x => x.Test_Id.Equals(test.Id));
            if (question == null)
            {
                return Json(new
                {
                    success = false,
                    errorMessage = "No questions in test " + test.Name
                }, JsonRequestBehavior.AllowGet);
            }

            CourseRun courseRun = new CourseRun
            {
                Progress = 50,
                User = u,
                TimeSpent = 100,
                Course = course
            };

            ThemeRun themeRun = new ThemeRun
            {
                Progress = 35,
                Theme = theme,
                CourseRun = courseRun
            };

            LectureRun lectureRun = new LectureRun
            {
                Lecture = lecture,
                TimeSpent = 20,
                ThemeRun = themeRun
            };

            TestRun testRun = new TestRun
            {
                Result = 1,
                Test = test,
                ThemeRun = themeRun,
                TestDateTime = DateTime.Now
            };

            QuestionRun questionRun = new QuestionRun
            {
                Question = question,
                TimeSpent = 100,
                TestRun = testRun
            };

            ParagraphRun paragraphRun = new ParagraphRun
            {
                HaveSeen = true,
                Paragraph = paragraph,
                LectureRun = lectureRun
            };

            context.QuestionRun.Add(questionRun);
            context.ThemeRun.Add(themeRun);
            context.TestRun.Add(testRun);
            context.LectureRun.Add(lectureRun);
            context.ParagraphRun.Add(paragraphRun);
            context.CourseRun.Add(courseRun);

            context.SaveChanges();
            return Json(new 
            {
                success = true
            }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ToggleGameAchievement(string user, int index)
        {
            User u = context.User.FirstOrDefault(x => x.Name == user);
            if (u == null)
                return Json(new
                {
                    success = false,
                    errorMessage = "User with name " + user + " not found"
                }, JsonRequestBehavior.AllowGet);

            GameAchievement achievement = context.GameAchievements.FirstOrDefault(x => x.Index == index);
            if (achievement == null)
                return Json(new
                {
                    success = false,
                    errorMessage = "Achievement with index " + index + " not found"
                }, JsonRequestBehavior.AllowGet);

            GameAchievementRun run = context.GameAchievementRuns.FirstOrDefault(x => x.UserId == u.Id 
                && x.GameAchievementId == achievement.Id);
            if (run == null)
            {
                run = new GameAchievementRun();
                run.GameAchievement = achievement;
                run.User = u;
                run.Passed = true;
                context.GameAchievementRuns.Add(run);
            }
            else
            {
                run.Passed = !run.Passed;
            }
            
            context.SaveChanges();
            return Json(new
            {
                success = true
            }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CalculateRatingForUser(string name)
        {
            var userRating = new UserRating(context, context.User.First(x => x.Name == name).Id);
            return Json(new
            {
                rating = userRating.CalculateRating()
            }, JsonRequestBehavior.AllowGet);
        }
    }
}
