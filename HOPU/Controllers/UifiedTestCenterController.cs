﻿using HOPU.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using X.PagedList;

namespace HOPU.Controllers
{
    public class UifiedTestCenterController : Controller
    {
        //public UifiedTestCenterController()
        //{

        //}
        //private readonly IRepository<ClassifieTypedBrowseViewModel> _iRepository;

        //public UifiedTestCenterController(IRepository<ClassifieTypedBrowseViewModel> iRepository)
        //{
        //    _iRepository = iRepository;
        //}

        #region 统测列表 UnifiedTestType
        public ActionResult UnifiedTestType(int? page)
        {
            var products = GetTestInfo().ToList();
            var pageNumber = page ?? 1;
            var onePage = products.ToPagedList(pageNumber, 5);
            var list = GetCourseInfo().ToList();
            var vms = list.Select(x => new CourseNameViewModel
            {
                CourseId = x.CourseId,
                CourseName = x.CourseName,
                TypeName = x.TypeName,
                TId = x.TId
            });
            var vmu = onePage.Select(x => new UniteTest
            {
                UtId = x.UtId,
                StartTime = x.StartTime,
                TimeLenth = x.TimeLenth,
                TopicCount = x.TopicCount,
                CourseName = x.CourseName,
                CourseId = x.CourseName
            });
            var vm = new UnifiedTestTypeViewModel
            {
                CourseNames = vms,
                UniteTests = vmu
            };
            return View(vm);
        }

        protected static IEnumerable<UniteTest> GetTestInfo()
        {

            HopuDBDataContext db = new HopuDBDataContext();
            var result = db.UniteTest.Select(a => a).OrderByDescending(a => a.UtId);
            return result;
        }

        #endregion

        #region 统测 UnifiedTest
        //public ActionResult UnifiedTest()
        //{
        //    return View();

        //}

        public ActionResult UnifiedTest(int? Id)
        {
            HopuDBDataContext db = new HopuDBDataContext();
            int? UtId = Id;
            bool joinUniteTest = false;
            if (UtId == null)
            {
                return PartialView("Error");
            }
            var result = db.UniteTest.Where(a => a.UtId == UtId).Select(a => a);
            List<UniteTest> timeInfo = result.ToList();//时间等信息
            ViewBag.timeInfo = timeInfo;
            ViewBag.Title = UtId;
            //能否进入考试
            if (timeInfo.Count() == 0)
            {
                return HttpNotFound();
            }
            foreach (var item in result)
            {
                //如果结束时间大于当前时间，可以进入考试
                if (Convert.ToDateTime(item.StartTime).AddMinutes(item.TimeLenth) > DateTime.Now)
                {
                    joinUniteTest = true;
                    break;
                }
                else
                {
                    //如果因为考试已结束不能进入考试，
                    return RedirectToAction("Score", "ScoreCenter", new { Id });
                }

            }
            //如果是第一次提交答案
            var UniteTestScoreInfo = db.UniteTestScore.Where(a => a.UtId == UtId && a.UserName == User.Identity.GetUserName()).FirstOrDefault();
            if (UniteTestScoreInfo == null)
            {
                joinUniteTest = true;
            }
            else
            {
                joinUniteTest = false;
                return RedirectToAction("Score", "ScoreCenter", new { Id });
            }
            //如果能加入统测
            if (joinUniteTest)
            {
                //据UtId获取题目列表
                int UserName = Convert.ToInt32(User.Identity.GetUserName());
                var topicListResult = db.UniteTestInfo.Where(a => a.UtId == UtId).ToList().Select(c =>
                new UniteTestInfo
                {
                    UtId = c.UtId,
                    Title = c.Title,
                    AnswerA = c.AnswerA,
                    AnswerB = c.AnswerB,
                    AnswerC = c.AnswerC,
                    AnswerD = c.AnswerD,
                    Answer = c.Answer,
                    CourseID = c.CourseID,
                    TopicID = (c.TopicID * (UserName - 200000000)) % 3 * 100,

                });
                if (UserName % 2 == 0)
                {
                    ViewBag.topicList = topicListResult.OrderBy(s => s.TopicID).ToList();
                }
                else
                {
                    ViewBag.topicList = topicListResult.OrderByDescending(s => s.TopicID).ToList();
                }
            }
            return View("UnifiedTest");

        }
        #endregion

        #region 校验统测答案
        //校验答案
        [HttpPost]
        public JsonResult UifiedTest(string[] Answer, int UtId)
        {
            HopuDBDataContext db = new HopuDBDataContext();
            //判断时间是否在考试时间段内
            bool commitAnswer = false;
            var timeInfo = db.UniteTest.Where(a => a.UtId == UtId).Select(a => a);
            foreach (var item in timeInfo)
            {
                //如果结束时间大于当前时间，可以提交
                if (Convert.ToDateTime(item.StartTime).AddMinutes(item.TimeLenth) > DateTime.Now.AddSeconds(-5))//给五秒的冗余时间 否则js倒计时0时提交会失败
                {
                    commitAnswer = true;
                    break;
                }
                else
                {
                    return Json(false);
                }

            }
            if (commitAnswer)
            {
                //据UtId获取答案
                int UserName = Convert.ToInt32(User.Identity.GetUserName());
                var topicListResult = db.UniteTestInfo.Where(a => a.UtId == UtId).ToList().Select(c =>
                new UniteTestInfo
                {
                    TopicID = (c.TopicID * (UserName - 200000000)) % 3 * 100,
                    Answer = c.Answer,
                });
                List<UniteTestInfo> answerList = new List<UniteTestInfo>();
                if (UserName % 2 == 0)
                {
                    answerList = topicListResult.OrderBy(s => s.TopicID).ToList();
                }
                else
                {
                    answerList = topicListResult.OrderByDescending(s => s.TopicID).ToList();
                }
                //校验答案
                List<UnifiedTestViewModel> result = new List<UnifiedTestViewModel>();
                //先算出每题多少分 
                double itemScore = 100F / answerList.Count;
                double SumScore = 0;
                //校验答案
                for (int i = 0; i < answerList.Count; i++)
                {
                    if (answerList[i].Answer.Equals(Answer[i]))
                    {
                        UnifiedTestViewModel resultinfo = new UnifiedTestViewModel
                        {
                            UserAnswer = Answer[i],
                            RealAnswer = answerList[i].Answer,
                            IsTrue = true
                        };
                        result.Add(resultinfo);
                        SumScore += itemScore;
                    }
                    else
                    {
                        UnifiedTestViewModel resultinfo = new UnifiedTestViewModel
                        {
                            UserAnswer = Answer[i],
                            RealAnswer = answerList[i].Answer,
                            IsTrue = false
                        };
                        result.Add(resultinfo);
                    }
                }
                var UniteTestScoreInfo = db.UniteTestScore.Where(a => a.UtId == UtId && a.UserName == User.Identity.GetUserName()).FirstOrDefault();
                //如果是第一次提交答案
                if (UniteTestScoreInfo == null)
                {
                    //将考试结果存入数据库
                    var uniteTestScore = new UniteTestScore
                    {
                        UtId = UtId,
                        RealUserName = GetRealUserName.GetRealName(User.Identity.GetUserId()),
                        UserName = User.Identity.GetUserName(),
                        EndTime = DateTime.Now,
                        Score = Convert.ToInt32(Math.Round(SumScore, 0, MidpointRounding.AwayFromZero))
                    };
                    db.UniteTestScore.InsertOnSubmit(uniteTestScore);
                    db.SubmitChanges();
                }
                else
                {
                    return Json(false);
                }
                return Json(result);
            }
            return Json(false);
        }
        #endregion

        #region 添加统测 AddTest
        //添加统测
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddTest(string[] submitCheckbox, int topicCount, int timeLenth)
        {
            HopuDBDataContext db = new HopuDBDataContext();

            //if (!IsAdmin())//如果不是admin权限
            if (!IsAdmin())//如果不是admin权限
            {
                return HttpNotFound();
            }
            else if (submitCheckbox.Count() == 0 || topicCount <= 0 || timeLenth <= 0)
            {
                return Json("不得留空！");
            }
            //生成where、courseName、CourseId的字符串
            string strCourseId = "", courseName = "", addSelectTopic = "";
            for (int i = 0; i < submitCheckbox.Length; i++)
            {
                strCourseId += submitCheckbox[i] + " ";
                addSelectTopic += "CourseId = " + submitCheckbox[i];
                //把courseName挨个儿查出来
                var dbCourseName = db.Course.Where(a => a.CourseID == Convert.ToDouble(submitCheckbox[i])).Select(a => a.CourseName).ToArray();
                courseName += dbCourseName[0];
                if (i < submitCheckbox.Length - 1)
                {
                    addSelectTopic += " or ";
                    courseName += "|";
                }
            }
            //以上步骤正常的话按要求获取题目
            string selectTopicSql = "select top " + topicCount + " * from Topic where " + addSelectTopic + " order by NEWID()";
            var topicList = db.ExecuteQuery<Topic>(selectTopicSql).ToList();
            //DataTable dt = SQLHelper.GetTable(selectTopicSql);
            if (topicList.Count < topicCount)//如果所取题目总数小于要求的数量
            {
                return Json("所填数量不得大于实际数量" + topicList.Count);
            }
            //获取最大统测UtId
            int realMaxUtid = 0;
            var maxUtid = db.UniteTest.Select(a => a.UtId);
            if (maxUtid.Count() == 0)
            {
                realMaxUtid = 1;
            }
            else
            {
                realMaxUtid = Convert.ToInt32(maxUtid.Max() + 1);
            }
            //向uniteTestInfo表添加题目信息
            List<UniteTestInfo> uniteTestInfos = new List<UniteTestInfo>();
            foreach (var s in topicList)
            {
                UniteTestInfo u = new UniteTestInfo
                {
                    UtId = realMaxUtid,
                    Title = s.Title,
                    TopicID = Convert.ToInt32(s.TopicID),
                    AnswerA = s.AnswerA,
                    AnswerB = s.AnswerB,
                    AnswerC = s.AnswerC,
                    AnswerD = s.AnswerD,
                    Answer = s.AnswerA,
                    CourseID = s.CourseID.ToString()
                };
                uniteTestInfos.Add(u);
            }
            //想UniteTest表添加统测信息
            var newTest = new UniteTest
            {
                UtId = realMaxUtid,
                StartTime = DateTime.Now,
                TimeLenth = timeLenth,
                TopicCount = topicCount,
                CourseId = strCourseId,
                CourseName = courseName
            };
            db.UniteTest.InsertOnSubmit(newTest);
            db.UniteTestInfo.InsertAllOnSubmit(uniteTestInfos);
            db.SubmitChanges();
            return Json(new { Success = true });
        }
        #endregion

        #region 删除统测
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public JsonResult DeleteTest(int UtId)
        {
            using (var db = new HopuDBDataContext())
            {

                if (!IsAdmin())//如果不是admin权限
                {
                    return Json(null);
                }
                //把要删除的统测题目查出来
                var testInfo = db.UniteTestInfo.Where(a => a.UtId == UtId).Select(a => a);
                //把要删除的统测查出来
                var test = db.UniteTest.SingleOrDefault(a => a.UtId == UtId);
                //把要删除的成绩信息查出来
                var scoreInfo = db.UniteTestScore.Where(a => a.UtId == UtId);
                if (test == null || testInfo == null)
                {
                    return Json(UtId + "不存在！");
                }
                else
                {
                    db.UniteTestScore.DeleteAllOnSubmit(scoreInfo);
                    db.UniteTestInfo.DeleteAllOnSubmit(testInfo);
                    db.UniteTest.DeleteOnSubmit(test);
                    db.SubmitChanges();
                }
            }
            return Json(new { Success = true });
        }
        #endregion

        #region 分类表 GetCourseInfo

        //获取分类表
        protected static IQueryable<CourseNameViewModel> GetCourseInfo()
        {
            HopuDBDataContext db = new HopuDBDataContext();
            var result = from p in db.TypeInfo
                         join c in db.Course on p.TID equals c.TID
                         orderby p.TID
                         select new CourseNameViewModel
                         {
                             TId = p.TID,
                             TypeName = p.TypeName,
                             CourseId = c.CourseID,
                             CourseName = c.CourseName
                         };
            return result;
        }

        #endregion

        #region 权限检测 IsAdmin?

        public bool IsAdmin()
        {
            if (GetUserType.GetUserTypeInfo(User.Identity.GetUserId()))//如果是admin权限
            {
                return true;
            }
            else
            {
                return false;
            }


        }

        #endregion
    }
}