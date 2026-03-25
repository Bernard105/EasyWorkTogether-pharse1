public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
{
    var normalizedEmail = request.Email.Trim().ToLower();
    
    var user = (await _unitOfWork.Users.FindAsync(u => u.Email == normalizedEmail)).FirstOrDefault();
    if (user == null)
    {
        // Don't reveal if email exists or not for security
        return;
    }
    
    // Invalidate any existing pending resets
    var existingResets = await _unitOfWork.PasswordResets.FindAsync(pr => 
        pr.Email == normalizedEmail && pr.Status == "pending");
    
    foreach (var reset in existingResets)
    {
        reset.Status = "expired";
        reset.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.PasswordResets.UpdateAsync(reset);
    }
    
    // Generate reset code (6-digit number for simplicity)
    var resetCode = new Random().Next(100000, 999999).ToString();
    var codeHash = _passwordService.HashPassword(resetCode);
    
    var passwordReset = new PasswordReset
    {
        Email = normalizedEmail,
        CodeHash = codeHash,
        ExpiresAt = DateTime.UtcNow.AddMinutes(15),
        Status = "pending",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
    
    await _unitOfWork.PasswordResets.AddAsync(passwordReset);
    await _unitOfWork.CompleteAsync();
    
    await _auditService.LogAsync(null, "PasswordReset", passwordReset.Id, "password_reset_requested");
    
    // In production, send email here
    // await _emailService.SendResetCodeAsync(normalizedEmail, resetCode);
    
    // For development, we can log the code
    Console.WriteLine($"Reset code for {normalizedEmail}: {resetCode}");
}

public async Task ResetPasswordAsync(ResetPasswordRequest request)
{
    var normalizedEmail = request.Email.Trim().ToLower();
    
    // Find pending reset
    var passwordReset = (await _unitOfWork.PasswordResets.FindAsync(pr => 
        pr.Email == normalizedEmail && 
        pr.Status == "pending" && 
        pr.ExpiresAt > DateTime.UtcNow)).FirstOrDefault();
    
    if (passwordReset == null)
    {
        throw new InvalidOperationException("Invalid or expired reset code");
    }
    
    // Verify code
    if (!_passwordService.VerifyPassword(request.Code, passwordReset.CodeHash))
    {
        throw new ArgumentException("Invalid reset code");
    }
    
    // Update user password
    var user = (await _unitOfWork.Users.FindAsync(u => u.Email == normalizedEmail)).FirstOrDefault();
    if (user == null)
    {
        throw new InvalidOperationException("User not found");
    }
    
    user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
    user.UpdatedAt = DateTime.UtcNow;
    await _unitOfWork.Users.UpdateAsync(user);
    
    // Mark reset as used
    passwordReset.Status = "used";
    passwordReset.UsedAt = DateTime.UtcNow;
    passwordReset.UpdatedAt = DateTime.UtcNow;
    await _unitOfWork.PasswordResets.UpdateAsync(passwordReset);
    
    // Revoke all sessions
    var sessions = await _unitOfWork.UserSessions.FindAsync(s => s.UserId == user.Id && s.Status == "active");
    foreach (var session in sessions)
    {
        session.Status = "revoked";
        session.RevokedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.UserSessions.UpdateAsync(session);
    }
    
    await _unitOfWork.CompleteAsync();
    
    await _auditService.LogAsync(user.Id, "User", user.Id, "password_reset_completed");
}