﻿// 
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// 
using System;
using System.Collections.Generic;
using System.Globalization;
using DotNetNuke.Common;
using DotNetNuke.Common.Internal;
using DotNetNuke.Entities.Friends;
using DotNetNuke.Services.Localization;
using DotNetNuke.Services.Social.Notifications;

namespace DotNetNuke.Entities.Users.Social.Internal
{
    internal class FriendsControllerImpl : IFriendsController
    {
        internal const string FriendRequest = "FriendRequest";


        //static FriendsControllerImpl()
        //{
        //}

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// AcceptFriend - Current User accepts a Friend Request to the Target User
        /// </summary>                
        /// <param name="targetUser">UserInfo for Target User</param>        
        /// <returns>UserRelationship object</returns>
        /// -----------------------------------------------------------------------------
        public void AcceptFriend(UserInfo targetUser)
        {
            var initiatingUser = UserController.Instance.GetCurrentUserInfo();
            var friendRelationship = RelationshipController.Instance.GetFriendRelationship(initiatingUser, targetUser);

            RelationshipController.Instance.AcceptUserRelationship(friendRelationship.UserRelationshipId);
            NotificationsController.Instance.DeleteNotificationRecipient(
                NotificationsController.Instance.GetNotificationType(FriendRequest).NotificationTypeId,
                friendRelationship.UserRelationshipId.ToString(CultureInfo.InvariantCulture), initiatingUser.UserID);

            EventManager.Instance.OnFriendshipAccepted(new RelationshipEventArgs(friendRelationship,initiatingUser.PortalID));
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// AddFriend - Current User initiates a Friend Request to the Target User
        /// </summary>                
        /// <param name="targetUser">UserInfo for Target User</param>        
        /// <returns>UserRelationship object</returns>
        /// <remarks>If the Friend Relationship is setup for auto-acceptance at the Portal level, the UserRelationship
        /// status is set as Accepted, otherwise it is set as Initiated.
        /// </remarks>
        /// -----------------------------------------------------------------------------
        public void AddFriend(UserInfo targetUser)
        {
            var initiatingUser = UserController.Instance.GetCurrentUserInfo();
            AddFriend(initiatingUser, targetUser);
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// AddFriend - Initiating User initiates a Friend Request to the Target User
        /// </summary>        
        /// <param name="initiatingUser">UserInfo for Initiating User</param>        
        /// <param name="targetUser">UserInfo for Target User</param>        
        /// <returns>UserRelationship object</returns>
        /// <remarks>If the Friend Relationship is setup for auto-acceptance at the Portal level, the UserRelationship
        /// status is set as Accepted, otherwise it is set as Initiated.
        /// </remarks>
        /// -----------------------------------------------------------------------------
        public void AddFriend(UserInfo initiatingUser, UserInfo targetUser)
        {
            Requires.NotNull("user1", initiatingUser);

            //Check if the friendship has been requested first by target user
            var targetUserRelationship = RelationshipController.Instance.GetFriendRelationship(targetUser,
                initiatingUser);
            if (targetUserRelationship != null && targetUserRelationship.Status == RelationshipStatus.Pending)
            {
                RelationshipController.Instance.AcceptUserRelationship(targetUserRelationship.UserRelationshipId);
                return;
            }

            var userRelationship = RelationshipController.Instance.InitiateUserRelationship(initiatingUser, targetUser, 
                                        RelationshipController.Instance.GetFriendsRelationshipByPortal(initiatingUser.PortalID));

            AddFriendRequestNotification(initiatingUser, targetUser, userRelationship);

            EventManager.Instance.OnFriendshipRequested(new RelationshipEventArgs(userRelationship, initiatingUser.PortalID));
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// DeleteFriend - Current User deletes a friend relationship with the target User
        /// </summary>
        /// <param name="targetUser">UserInfo for Target User</param>        
        /// -----------------------------------------------------------------------------
        public void DeleteFriend(UserInfo targetUser)
        {
            var initiatingUser = UserController.Instance.GetCurrentUserInfo();
            DeleteFriend(initiatingUser, targetUser);
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// DeleteFriend - Initiating User deletes a friend relationship with the target User
        /// </summary>
        /// <param name="initiatingUser">UserInfo for Initiating User</param>        
        /// <param name="targetUser">UserInfo for Target User</param>        
        /// -----------------------------------------------------------------------------
        public void DeleteFriend(UserInfo initiatingUser, UserInfo targetUser)
        {
            Requires.NotNull("user1", initiatingUser);

            var friend = RelationshipController.Instance.GetUserRelationship(initiatingUser, targetUser,
                RelationshipController.Instance.GetFriendsRelationshipByPortal(initiatingUser.PortalID));

            RelationshipController.Instance.DeleteUserRelationship(friend);

            EventManager.Instance.OnFriendshipDeleted(new RelationshipEventArgs(friend, initiatingUser.PortalID));
        }

        private static void AddFriendRequestNotification(UserInfo initiatingUser, UserInfo targetUser, UserRelationship userRelationship)
        {
            var notificationType = NotificationsController.Instance.GetNotificationType(FriendRequest);
            var language = GetUserPreferredLocale(targetUser)?.Name;
            var subject = string.Format(Localization.GetString("AddFriendRequestSubject", Localization.GlobalResourceFile, language),
                              initiatingUser.DisplayName);

            var body = string.Format(Localization.GetString("AddFriendRequestBody", Localization.GlobalResourceFile, language),
                              initiatingUser.DisplayName);

            var notification = new Notification
            {
                NotificationTypeID = notificationType.NotificationTypeId,
                Subject = subject,
                Body = body,
                IncludeDismissAction = true,
                Context = userRelationship.UserRelationshipId.ToString(CultureInfo.InvariantCulture),
                SenderUserID = initiatingUser.UserID
            };

            NotificationsController.Instance.SendNotification(notification, initiatingUser.PortalID, null, new List<UserInfo> { targetUser });
        }

        private static CultureInfo GetUserPreferredLocale(UserInfo user)
        {
            string language = user.Profile.PreferredLocale;
            if (!string.IsNullOrEmpty(language))
            {
                return Localization.GetCultureFromString(user.PortalID, language);
            }

            return null;
        }
    }
}
