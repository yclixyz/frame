using System.Net.Http;

namespace Gugubao.Main
{
    interface IAccountService
    {
        void Accounts();
    }

    public class AccountService : IAccountService
    {
        private readonly HttpClient _httpClient;

        public AccountService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public void Accounts()
        {
            _httpClient.GetAsync("");
        }
    }

}