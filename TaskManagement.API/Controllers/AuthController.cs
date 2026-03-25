[HttpPost("forgot-password")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
{
    try
    {
        await _authService.ForgotPasswordAsync(request);
        return NoContent();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during forgot password");
        return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
    }
}

[HttpPost("reset-password")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
{
    try
    {
        await _authService.ResetPasswordAsync(request);
        return Ok(new { message = "Password reset successful" });
    }
    catch (InvalidOperationException ex)
    {
        return BadRequest(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during password reset");
        return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
    }
}