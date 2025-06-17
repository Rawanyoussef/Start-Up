using StartUP.Data.Entity;
using StartUP.Repository.ProjectRepo;
using StartUP.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StartUP.Repository.UserRepo;
using Microsoft.AspNetCore.Http;

namespace StartUP.Service
{


    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepo;
        private readonly IProjecRepo _projectRepo;
        private readonly IUserRepo _userRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public NotificationService(
            INotificationRepository notificationRepo,
            IProjecRepo projectRepo,
            IUserRepo userRepo, IHttpContextAccessor httpContextAccessor)
        {
            _notificationRepo = notificationRepo;
            _projectRepo = projectRepo;
            _userRepo = userRepo;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IEnumerable<NotificationDTO>> GetNonReadNotificationsAsyncAsync(int userId)
        {
            var baseUrl = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}";
            var notifications = await _notificationRepo.GetAllNotificationsByUserIdAsync(userId);

            return notifications.Select(n => new NotificationDTO
            {
                Id = n.Id,
                SenderName =n.Sender.Name,
                SenderPhoto = string.IsNullOrEmpty(n.Sender.Image)
                    ? null
                    : $"{baseUrl}/images/{n.Sender.Image}",
              
                ProjectName = n.Project?.ProjectName,
                ProjectId =n.ProjectId,
                Message = n.Message,
                FullMessage = n.FullMessage?
                    .Replace("\r\n", " ")
                    .Replace("\n", " ")
                    .Replace("\r", " "),


                CreatedAt = n.CreatedAt,
                IsUnread = n.IsUnread,
                Type= n.Type,
                Status = n.Status
            });
        }

        public async Task<IEnumerable<NotificationDTO>> GetReadNotificationsAsyncAsync(int userId)
        {
            var baseUrl = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}";
            var notifications = await _notificationRepo.GetReadNotificationsByUserIdAsync(userId);

            return notifications.Select(n => new NotificationDTO
            {

                Id = n.Id,
                SenderName = n.Sender.Name,
                SenderPhoto = string.IsNullOrEmpty(n.Sender.Image)
                    ? null
                    : $"{baseUrl}/images/{n.Sender.Image}",

                ProjectName = n.Project?.ProjectName,
                ProjectId = n.ProjectId,
                Message = n.Message,
                FullMessage = n.FullMessage?
                    .Replace("\r\n", " ")
                    .Replace("\n", " ")
                    .Replace("\r", " "),


                CreatedAt = n.CreatedAt,
                IsUnread = n.IsUnread,
                Type = n.Type,
                Status = n.Status
            });
        }
        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _notificationRepo.GetByIdAsync(notificationId);
            if (notification != null && notification.IsUnread)
            {
                notification.IsUnread = false;
                await _notificationRepo.UpdateAsync(notification);
            }
            else { throw new Exception("Notification Not Found"); }
        }
        public async Task DeleteAsync(int notificationId)
        {
            await _notificationRepo.DeleteAsync(notificationId);
        }
        public async Task MarkAllAsReadAsync(int userId)
        {
            try
            {
                var notifications = await _notificationRepo.GetNonReadNotificationsByUserIdAsync(userId);

                if (notifications == null || !notifications.Any())
                {
                    throw new InvalidOperationException("No unread notifications found for this user.");
                }

                foreach (var notification in notifications.Where(n => n.IsUnread))
                {
                    notification.IsUnread = false;
                }

                await _notificationRepo.UpdateRangeAsync(notifications);
            }
            catch (Exception ex)
            {
                // يمكنك استخدام logger هنا إن عندك logging system
                throw new ApplicationException($"An error occurred while marking notifications as read for user {userId}.", ex);
            }
        }

        public async Task DeleteAllNotificationsAsync(int userId)
        {
            try
            {
                var notifications = await _notificationRepo.GetAllNotificationsByUserIdAsync(userId);

                if (notifications == null || !notifications.Any())
                {
                    throw new InvalidOperationException("No notifications found for this user.");
                }

                await _notificationRepo.DeleteRangeAsync(notifications);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"An error occurred while deleting notifications for user {userId}.", ex);
            }
        }


    }
}
    
