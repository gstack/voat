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

using System;
using System.Collections.Generic;
using Voat.Common;
using Voat.Data;

namespace Voat.Domain.Models
{
    public class UserInformation
    {
        /// <summary>
        /// Date that data was calculated
        /// </summary>
        public DateTime GenerationDate { get; set; } = Repository.CurrentDate;

        /// <summary>
        /// The badges the user has accumulated
        /// </summary>
        public IEnumerable<UserBadge> Badges { get; set; }

        /// <summary>
        /// Short bio of user
        /// </summary>
        public string Bio { get; set; }

        /// <summary>
        /// Comment Contribution Points (CCP)
        /// </summary>
        public Score CommentPoints { get; set; }

        /// <summary>
        /// Comment Voting Behavior (Only available if request is authenticated)
        /// </summary>
        public Score CommentVoting { get; set; }

        /// <summary>
        /// Path to profile picture
        /// </summary>
        public string ProfilePicture { get; set; }

        /// <summary>
        /// Date user registered
        /// </summary>
        public DateTime RegistrationDate { get; set; }

        /// <summary>
        /// Submission Contribution Points (SCP)
        /// </summary>
        public Score SubmissionPoints { get; set; }

        /// <summary>
        /// Submission Voting Distribution (Only available if request is authenticated)
        /// </summary>
        public Score SubmissionVoting { get; set; }

        /// <summary>
        /// The user name of the user when addressed by name
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// The subverses the user moderates
        /// </summary>
        public IEnumerable<SubverseModerator> Moderates { get; set; }
    }
}
