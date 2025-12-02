using System.Text;
using ECommerce.Notifications.Api.Data;
using ECommerce.Notifications.Api.Models;
using ECommerce.Shared.Contracts.Events;
using MailKit.Net.Smtp;
using MimeKit;

namespace ECommerce.Notifications.Api.Services;

/// <summary>
/// Service for sending email notifications.
/// </summary>
public class EmailService
{
    private readonly NotificationsDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        NotificationsDbContext dbContext,
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Sends an order confirmation email.
    /// </summary>
    public async Task SendOrderConfirmationAsync(OrderCreatedEvent orderEvent)
    {
        var subject = $"Order Confirmation - {orderEvent.OrderNumber}";
        var body = GenerateOrderConfirmationEmail(orderEvent);

        var emailLog = new EmailLog
        {
            Recipient = orderEvent.UserEmail,
            Subject = subject,
            BodyPreview = $"Thank you for your order #{orderEvent.OrderNumber}",
            Status = EmailStatus.Pending
        };

        _dbContext.EmailLogs.Add(emailLog);
        await _dbContext.SaveChangesAsync();

        try
        {
            await SendEmailAsync(orderEvent.UserEmail, subject, body);

            emailLog.Status = EmailStatus.Sent;
            emailLog.SentAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Order confirmation email sent for: {OrderNumber}", orderEvent.OrderNumber);
        }
        catch (Exception ex)
        {
            emailLog.Status = EmailStatus.Failed;
            emailLog.ErrorMessage = ex.Message;
            await _dbContext.SaveChangesAsync();

            _logger.LogError(ex, "Failed to send order confirmation email for: {OrderNumber}", orderEvent.OrderNumber);
        }
    }

    private async Task SendEmailAsync(string recipient, string subject, string htmlBody)
    {
        var smtpHost = _configuration["Smtp:Host"] ?? "localhost";
        var smtpPort = int.Parse(_configuration["Smtp:Port"] ?? "1025");
        var smtpUser = _configuration["Smtp:User"] ?? "";
        var smtpPass = _configuration["Smtp:Password"] ?? "";
        var senderEmail = _configuration["Smtp:SenderEmail"] ?? "noreply@ecommerce.demo";
        var senderName = _configuration["Smtp:SenderName"] ?? "E-Commerce Demo";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(senderName, senderEmail));
        message.To.Add(new MailboxAddress("", recipient));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlBody
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        
        try
        {
            await client.ConnectAsync(smtpHost, smtpPort, false);

            if (!string.IsNullOrEmpty(smtpUser))
            {
                await client.AuthenticateAsync(smtpUser, smtpPass);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SMTP send failed. Email logged but not delivered. SMTP might not be configured.");
            // In development without SMTP server, we don't re-throw - the email is logged for auditing
            // This allows the application to continue functioning without an SMTP server configured
        }
    }

    private string GenerateOrderConfirmationEmail(OrderCreatedEvent orderEvent)
    {
        var itemsHtml = new StringBuilder();
        foreach (var item in orderEvent.Items)
        {
            itemsHtml.AppendLine($@"
                <tr>
                    <td style='padding: 10px; border-bottom: 1px solid #ddd;'>{item.ProductName}</td>
                    <td style='padding: 10px; border-bottom: 1px solid #ddd; text-align: center;'>{item.Quantity}</td>
                    <td style='padding: 10px; border-bottom: 1px solid #ddd; text-align: right;'>${item.UnitPrice:F2}</td>
                    <td style='padding: 10px; border-bottom: 1px solid #ddd; text-align: right;'>${(item.Quantity * item.UnitPrice):F2}</td>
                </tr>");
        }

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Order Confirmation</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background-color: #0066cc; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0;'>
        <h1 style='margin: 0;'>🛒 E-Commerce Demo</h1>
    </div>
    
    <div style='background-color: #f9f9f9; padding: 30px; border: 1px solid #ddd; border-top: none;'>
        <h2 style='color: #28a745;'>✅ Order Confirmed!</h2>
        <p>Thank you for your order. Your order has been received and is being processed.</p>
        
        <div style='background-color: white; padding: 20px; border-radius: 8px; margin: 20px 0;'>
            <h3 style='color: #0066cc; margin-top: 0;'>Order Details</h3>
            <p><strong>Order Number:</strong> {orderEvent.OrderNumber}</p>
            <p><strong>Order Date:</strong> {orderEvent.Timestamp:MMMM dd, yyyy 'at' HH:mm}</p>
        </div>
        
        <div style='background-color: white; padding: 20px; border-radius: 8px; margin: 20px 0;'>
            <h3 style='color: #0066cc; margin-top: 0;'>Items Ordered</h3>
            <table style='width: 100%; border-collapse: collapse;'>
                <thead>
                    <tr style='background-color: #f5f5f5;'>
                        <th style='padding: 10px; text-align: left; border-bottom: 2px solid #ddd;'>Product</th>
                        <th style='padding: 10px; text-align: center; border-bottom: 2px solid #ddd;'>Qty</th>
                        <th style='padding: 10px; text-align: right; border-bottom: 2px solid #ddd;'>Price</th>
                        <th style='padding: 10px; text-align: right; border-bottom: 2px solid #ddd;'>Subtotal</th>
                    </tr>
                </thead>
                <tbody>
                    {itemsHtml}
                </tbody>
                <tfoot>
                    <tr style='font-weight: bold; background-color: #f5f5f5;'>
                        <td colspan='3' style='padding: 10px; text-align: right;'>Total:</td>
                        <td style='padding: 10px; text-align: right; color: #0066cc;'>${orderEvent.TotalAmount:F2}</td>
                    </tr>
                </tfoot>
            </table>
        </div>
        
        <div style='background-color: white; padding: 20px; border-radius: 8px; margin: 20px 0;'>
            <h3 style='color: #0066cc; margin-top: 0;'>📦 Estimated Delivery</h3>
            <p>Your order should arrive within 5-7 business days.</p>
        </div>
        
        <p style='font-size: 14px; color: #666;'>If you have any questions about your order, please contact our support team.</p>
    </div>
    
    <div style='background-color: #333; color: white; padding: 20px; text-align: center; border-radius: 0 0 8px 8px;'>
        <p style='margin: 0; font-size: 12px;'>© 2024 E-Commerce Demo. This is a demonstration application.</p>
        <p style='margin: 10px 0 0 0; font-size: 12px;'>Built with .NET Aspire</p>
    </div>
</body>
</html>";
    }
}
