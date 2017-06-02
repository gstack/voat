#region LICENSE

/*
    
    Copyright(c) Voat, Inc.

    This file is part of Voat.

    This source file is subject to version 3 of the GPL license,
    that is bundled with this package in the file LICENSE, and is
    available online at http://www.gnu.org/licenses/gpl-3.0.txt;
    you may not use this file except in compliance with the License.

    Software distributed under the License is distributed on an
    "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either express
    or implied. See the License for the specific language governing
    rights and limitations under the License.

    All Rights Reserved.

*/

#endregion LICENSE

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;
using Voat.Caching;
using Voat.Data.Models;
using Voat.Domain.Command;
using Voat.Domain.Query;
using Voat.Tests.Repository;
using Voat.Utilities;

namespace Voat.Tests.CommandTests
{
    [TestClass]
    public class UserCommandTests : BaseUnitTest
    {

        [TestMethod]
        [TestCategory("User"), TestCategory("Command"), TestCategory("Query")]
        public async Task UpdateUserPreferences()
        {
            var userName = "TestUser5";
            var bio = Guid.NewGuid().ToString();
            TestHelper.SetPrincipal(userName);

            var q = new QueryUserPreferences(userName);
            var prefs = await q.ExecuteAsync();

            Assert.IsNotNull(prefs, "Pref query returned null");
            Assert.AreNotEqual(prefs.Bio, bio, "Bio returned unexpected data");

            var cmd = new UpdateUserPreferencesCommand(new Domain.Models.UserPreferenceUpdate() { Bio = bio });
            var r = await cmd.Execute();
            VoatAssert.IsValid(r);

            q = new QueryUserPreferences(userName);
            prefs = await q.ExecuteAsync();

            Assert.IsNotNull(prefs, "Pref requery returned null");
            Assert.AreEqual(prefs.Bio, bio, "Bio not updated");

        }

        [TestMethod]
        [TestCategory("User"), TestCategory("Command"), TestCategory("Query")]
        public async Task UserSaves()
        {
            var userName = "UnitTestUser20";
            var bio = Guid.NewGuid().ToString();
            TestHelper.SetPrincipal(userName);

            //save start test data
            using (var repo = new Voat.Data.Repository())
            {
                var r = await repo.Save(Domain.Models.ContentType.Submission, 1);
                VoatAssert.IsValid(r);

                r = await repo.Save(Domain.Models.ContentType.Comment, 1);
                VoatAssert.IsValid(r);

            }

            var q = new QueryUserSaves(Domain.Models.ContentType.Submission);
            var d = await q.ExecuteAsync();
            Assert.AreEqual(true, d.Contains(1));
            Assert.AreEqual(1, d.Count);
            Assert.AreEqual(true, CacheHandler.Instance.Exists(CachingKey.UserSavedItems(Domain.Models.ContentType.Submission, userName)));

            q = new QueryUserSaves(Domain.Models.ContentType.Comment);
            d = await q.ExecuteAsync();
            Assert.AreEqual(true, d.Contains(1));
            Assert.AreEqual(1, d.Count);
            Assert.AreEqual(true, CacheHandler.Instance.Exists(CachingKey.UserSavedItems(Domain.Models.ContentType.Comment, userName)));


            //check helper object
            Assert.AreEqual(true, UserHelper.IsSaved(Domain.Models.ContentType.Submission, 1));
            Assert.AreEqual(true, UserHelper.IsSaved(Domain.Models.ContentType.Comment, 1));

            Assert.AreEqual(false, UserHelper.IsSaved(Domain.Models.ContentType.Submission, 2));
            Assert.AreEqual(false, UserHelper.IsSaved(Domain.Models.ContentType.Comment, 2));


            var cmd = new SaveCommand(Domain.Models.ContentType.Submission, 2);
            var response = await cmd.Execute();
            VoatAssert.IsValid(response);


            cmd = new SaveCommand(Domain.Models.ContentType.Comment, 2);
            response = await cmd.Execute();
            VoatAssert.IsValid(response);

            Assert.AreEqual(true, UserHelper.IsSaved(Domain.Models.ContentType.Submission, 2));
            Assert.AreEqual(true, UserHelper.IsSaved(Domain.Models.ContentType.Comment, 2));

            cmd = new SaveCommand(Domain.Models.ContentType.Submission, 1);
            response = await cmd.Execute();
            VoatAssert.IsValid(response);

            cmd = new SaveCommand(Domain.Models.ContentType.Comment, 1);
            response = await cmd.Execute();
            VoatAssert.IsValid(response);

            Assert.AreEqual(false, UserHelper.IsSaved(Domain.Models.ContentType.Submission, 1));
            Assert.AreEqual(false, UserHelper.IsSaved(Domain.Models.ContentType.Comment, 1));

        }

        [TestMethod]
        [TestCategory("User"), TestCategory("Command"), TestCategory("User.DeleteAccount")]
        public async Task DeleteAccount_Failures()
        {
            var userName = "TestDeleteUserBad";

            VoatDataInitializer.CreateUser(userName);

            TestHelper.SetPrincipal(userName);
            //username doesnt match
            var cmd = new DeleteAccountCommand(
                new Domain.Models.DeleteAccountOptions()
                {
                    UserName = userName,
                    ConfirmUserName = "Tom"
                });
            var result = await cmd.Execute();
            Assert.IsFalse(result.Success, result.Message);

            //password doesn't match
            cmd = new DeleteAccountCommand(
                new Domain.Models.DeleteAccountOptions()
                {
                    UserName = userName,
                    ConfirmUserName = userName,
                    CurrentPassword = "NotCorrect"
                });
            result = await cmd.Execute();
            Assert.IsFalse(result.Success, result.Message);

            //wrong user
            TestHelper.SetPrincipal("TestUser01");

            cmd = new DeleteAccountCommand(
                new Domain.Models.DeleteAccountOptions()
                {
                    UserName = userName,
                    ConfirmUserName = userName,
                    CurrentPassword = userName
                });
            result = await cmd.Execute();
            Assert.IsFalse(result.Success, result.Message);


        }

        [TestMethod]
        [TestCategory("User"), TestCategory("Command"), TestCategory("User.DeleteAccount")]
        public async Task DeleteAccount_Basic()
        {
            //EnsureBadges
            using (var db = new VoatDataContext())
            {
                if (!db.Badge.Any(x => x.ID == "deleted"))
                {
                    db.Badge.Add(new Badge() { ID = "deleted", Name = "Account Deleted", Graphic = "deleted.png", Title = "deleted" }); 
                }
                if (!db.Badge.Any(x => x.ID == "deleted2"))
                {
                    db.Badge.Add(new Badge() { ID = "deleted2", Name = "Account Deleted", Graphic = "deleted2.png", Title = "deleted" });
                }
                if (!db.Badge.Any(x => x.ID == "donor_upto_30"))
                {
                    db.Badge.Add(new Badge() { ID = "donor_upto_30", Name = "Donor Up To Thirty", Graphic = "donor30.png", Title = "Donor" });
                }

                
                db.SaveChanges();
            }


            var userName = "TestDeleteUser01";

            VoatDataInitializer.CreateUser(userName);
            TestHelper.SetPrincipal(userName);
            DeleteAccountCommand cmd;
            CommandResponse result;

            var options = new Domain.Models.DeleteAccountOptions()
            {
                UserName = userName,
                ConfirmUserName = userName,
                CurrentPassword = userName,
                Comments = Domain.Models.DeleteOption.Anonymize,
                LinkSubmissions = Domain.Models.DeleteOption.Anonymize,
                TextSubmissions = Domain.Models.DeleteOption.Anonymize
            };

            var submission = TestHelper.ContentCreation.CreateSubmission(options.UserName, new Domain.Models.UserSubmission()
            {
                Subverse = "unit",
                Title = "Test Submission Yeah",
                Content = "Test Submission Content"
            });

            var comment = TestHelper.ContentCreation.CreateComment(options.UserName, submission.ID, "This is a test comment");

            //Anon it all 
            cmd = new DeleteAccountCommand(options);
            result = await cmd.Execute();
            VoatAssert.IsValid(result);
            VerifyDelete(options);


            userName = "TestDeleteUser02";

            VoatDataInitializer.CreateUser(userName);
            TestHelper.SetPrincipal(userName);

            using (var db = new VoatDataContext())
            {
                //Trying to trap a bug with a user not getting delete badge
                db.UserBadge.Add(new Voat.Data.Models.UserBadge() { BadgeID = "donor_upto_30", CreationDate = DateTime.UtcNow, UserName = userName });
                db.SaveChanges();
            }

            options = new Domain.Models.DeleteAccountOptions()
            {
                UserName = userName,
                ConfirmUserName = userName,
                CurrentPassword = userName,
                Comments = Domain.Models.DeleteOption.Delete,
                LinkSubmissions = Domain.Models.DeleteOption.Delete,
                TextSubmissions = Domain.Models.DeleteOption.Delete
            };

            submission = TestHelper.ContentCreation.CreateSubmission(options.UserName, new Domain.Models.UserSubmission()
            {
                Subverse = "unit",
                Title = "Test Submission Yeah",
                Content = "Test Submission Content"
            });

            comment = TestHelper.ContentCreation.CreateComment(options.UserName, submission.ID, "This is a test comment");

            //Delete
            cmd = new DeleteAccountCommand(options);
            result = await cmd.Execute();
            VoatAssert.IsValid(result);

            VerifyDelete(options);



            userName = "TestDeleteUser03";

            VoatDataInitializer.CreateUser(userName);
            TestHelper.SetPrincipal(userName);

            //Need to ensure delete clears preferences
            var prefUpdate = new UpdateUserPreferencesCommand(new Domain.Models.UserPreferenceUpdate() { Bio = "My Bio" });
            var p = await prefUpdate.Execute();
            VoatAssert.IsValid(p);

            using (var db = new VoatDataContext())
            {
                var prefs = db.UserPreference.FirstOrDefault(x => x.UserName == userName);
                Assert.IsNotNull(prefs, "Expected user to have preference record at this stage");
                prefs.Avatar = userName + ".jpg";

                //Add badges to prevent duplicates
                db.UserBadge.Add(new Voat.Data.Models.UserBadge() { BadgeID = "deleted", CreationDate = DateTime.UtcNow, UserName = userName });
                db.UserBadge.Add(new Voat.Data.Models.UserBadge() { BadgeID = "deleted2", CreationDate = DateTime.UtcNow, UserName = userName });

                db.SaveChanges();

            }

            options = new Domain.Models.DeleteAccountOptions()
            {
                UserName = userName,
                ConfirmUserName = userName,
                CurrentPassword = userName,
                Comments = Domain.Models.DeleteOption.Delete,
                LinkSubmissions = Domain.Models.DeleteOption.Delete,
                TextSubmissions = Domain.Models.DeleteOption.Delete,
                RecoveryEmailAddress = "bads@motherfather.com",
                ConfirmRecoveryEmailAddress = "bads@motherfather.com",
                Reason = "I need a break from the racialists"
            };

            submission = TestHelper.ContentCreation.CreateSubmission(options.UserName, new Domain.Models.UserSubmission()
            {
                Subverse = "unit",
                Title = "Test Submission Yeah",
                Content = "Test Submission Content"
            });

            comment = TestHelper.ContentCreation.CreateComment(options.UserName, submission.ID, "This is a test comment");

            //Delete
            cmd = new DeleteAccountCommand(options);
            result = await cmd.Execute();
            VoatAssert.IsValid(result);
            VerifyDelete(options);

        }


        private void VerifyDelete(Domain.Models.DeleteAccountOptions options)
        {
            using (var db = new Voat.Data.Models.VoatDataContext())
            {
                int count = 0;
                switch (options.Comments.Value)
                {
                    case Domain.Models.DeleteOption.Anonymize:

                        count = db.Comment.Count(x => x.UserName.Equals(options.UserName, StringComparison.OrdinalIgnoreCase) && !x.IsAnonymized);
                        Assert.AreEqual(0, count, $"Comment {options.Comments.ToString()} setting found violations");

                        break;
                    case Domain.Models.DeleteOption.Delete:

                        count = db.Comment.Count(x => x.UserName.Equals(options.UserName, StringComparison.OrdinalIgnoreCase) && !x.IsDeleted);
                        Assert.AreEqual(0, count, $"Comment {options.Comments.ToString()} setting found violations");
                        break;
                }

                var checkSubmissions = new Action<string, Domain.Models.SubmissionType, Domain.Models.DeleteOption>((userName, submissionType, deleteSetting) =>
                {
                    switch (deleteSetting)
                    {
                        case Domain.Models.DeleteOption.Anonymize:

                            count = db.Submission.Count(x => x.UserName.Equals(options.UserName, StringComparison.OrdinalIgnoreCase) && !x.IsAnonymized && x.Type == (int)submissionType);
                            Assert.AreEqual(0, count, $"{submissionType.ToString()} Submission {deleteSetting.ToString()} setting found violations");

                            break;
                        case Domain.Models.DeleteOption.Delete:

                            count = db.Submission.Count(x => x.UserName.Equals(options.UserName, StringComparison.OrdinalIgnoreCase) && !x.IsDeleted && x.Type == (int)submissionType);
                            Assert.AreEqual(0, count, $"{submissionType.ToString()} Submission {deleteSetting.ToString()} setting found violations");
                            break;
                    }
                });

                checkSubmissions(options.UserName, Domain.Models.SubmissionType.Text, options.TextSubmissions);
                checkSubmissions(options.UserName, Domain.Models.SubmissionType.Link, options.LinkSubmissions);

                //Check account settings.
                using (var userManager = VoatUserManager.Create())
                {
                    var userAccount = userManager.FindByName(options.UserName);

                    Assert.IsNotNull(userAccount, "Can't find user account using manager");
                    if (!String.IsNullOrEmpty(options.RecoveryEmailAddress))
                    {
                        //Verify recovery info
                        Assert.AreEqual(userAccount.Email, options.RecoveryEmailAddress);
                        Assert.IsNotNull(userAccount.LockoutEnd, "Lockout should be enabled");
                        //CORE_PORT: Not available
                        throw new NotImplementedException("Core Port not implemented");
                        /*
                        Assert.IsNotNull(userAccount.LockoutEndDateUtc, "Lockout should not be null");
                        Assert.IsTrue(userAccount.LockoutEndDateUtc.Value.Subtract(DateTime.UtcNow) >= TimeSpan.FromDays(89), "Lockout be set to roughly 90 days");
                        */
                       
                    }
                    else
                    {
                        Assert.AreEqual(userAccount.Email, null);
                        Assert.IsNull(userAccount.LockoutEnd, "Lockout should not be enabled");
                    }

                  

                   //Make sure password is reset
                   var passwordAccess = userManager.Find(options.UserName, options.CurrentPassword);
                    Assert.IsNull(passwordAccess, "Can access user account with old password");

                }
                var badgeToCheck = String.IsNullOrEmpty(options.RecoveryEmailAddress) ? "deleted" : "deleted2";
                Assert.AreEqual(1, db.UserBadge.Count(x => x.UserName == options.UserName && x.BadgeID == badgeToCheck), "Can not find delete badge");

                //Verify Bio and Avatar cleared
                var prefs = db.UserPreference.Where(x => x.UserName.Equals(options.UserName, StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var pref in prefs)
                {
                    Assert.AreEqual(null, pref.Avatar, "Avatar not cleared");
                    Assert.AreEqual(null, pref.Bio, "Bio not cleared");
                }
            }
        }
    }
}
