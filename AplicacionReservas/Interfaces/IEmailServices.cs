namespace AplicacionReservas.Interfaces
{
    public interface IEmailServices
    {
        Task EnviarCorreoAsync(string toEmail, string subject, string body);

    }
}
