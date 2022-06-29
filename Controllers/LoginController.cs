using CAST.Business.Component.Security;
using Petrobras.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CAST.Application.Interfaces;
using System.Web.Security;
using System.Net;
using CAST.Business.Component;
using Newtonsoft.Json;
using CAST.Infrastructure;
using System.Runtime.Caching;
using System.Configuration;
using System.Web.Configuration;

namespace CAST.Controllers
{
    public class LoginController : BaseController
    {
        private readonly IControleAccesso _controleAcesso;
        private readonly ILoginAppService _loginAppService;

        public LoginController(IControleAccesso controleAcesso,
                                ILoginAppService loginAppService)
        {
            _controleAcesso          = controleAcesso;
            _loginAppService         = loginAppService;
        }
        public LoginController(){}

        // GET: /Login/
        public ActionResult Index(string RedirectUrl)
        {

            if (RedirectUrl != null)
            {
                TempData["RedirectUrl"] = RedirectUrl;
            }
                

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult ValidarLogin(string login, string senha)
        {
            string mensagem = string.Empty;            
            string jsonUsuario = string.Empty;

            try
            {
                if (Membership.ValidateUser(login.ToUpper(), senha))
                {

                    JsonSerializerSettings js = new JsonSerializerSettings();
                    js.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

                    var forcaTrabalho = _loginAppService.BuscarForcaTrabalho(login.ToUpper());

                    // Se estiver no cav4 é administrador geral
                    UsuarioSistema usuario = _controleAcesso.BuscarDadosUsuario(login.ToUpper());

                    usuario.CodUsuario = forcaTrabalho.Codigo;

                    if(usuario == null)
                    {
                        usuario.Chave = login.ToUpper();
                        usuario.CodUsuario = forcaTrabalho.Codigo;  //_loginAppService.BuscarForcaTrabalho(login.ToUpper()).Codigo;
                    }

                    // Se estiver na bidt e for gestor de um orgão
                    if (_loginAppService.VerificaGestorPorCodigoUsuario(forcaTrabalho.Codigo))
                    {
                        usuario.Perfis.Add(new PerfilAcesso { id = 2, nome = "Gestor", administrador = false, codigoPerfil = TipoPerfil.Gestor });
                    }

                    // Verifica se o usuario é adm de sistema
                    if (_loginAppService.VerificaAdministradorSistema(usuario.CodUsuario))
                    {
                        if (!usuario.ExecutaRelatorioChave)
                        {
                            usuario.ExecutaRelatorioChave = _loginAppService.VerificaExecucaoRelatorioChaves(usuario.CodUsuario);
                        }
                            

                        usuario.Perfis.Add(new PerfilAcesso { id = 3, nome = "Administrador de Sistema", administrador = true, codigoPerfil = TipoPerfil.AdministradorSistema });
                    }

                    // Verifica se é usuário Padrão
                    if (_loginAppService.VerificaUsuarioPadrao(usuario.Chave))
                    {
                        usuario.Perfis.Add(new PerfilAcesso { id = 4, nome = "Padrao", administrador = false, codigoPerfil = TipoPerfil.Padrao });
                    }

                    if (usuario.Perfis.Count > 0)
                    {
                        CriarCookie(login, usuario);
                        ConfiguracaoCookiesGeral();
                        jsonUsuario = JsonConvert.SerializeObject(usuario, Formatting.None, js);

                        return Json(new { Status = HttpStatusCode.OK, Mensagem = ""}, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        mensagem = "Chave não encontrada.";                        
                        return Json(new { Status = HttpStatusCode.BadRequest, Mensagem = mensagem }, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    mensagem = "Chave ou senha inválidos";                    
                    return Json(new { Status = HttpStatusCode.BadRequest, Mensagem = mensagem }, JsonRequestBehavior.AllowGet);
                }
            }
            
            catch (BusinessException ex)
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                return Json(new { Status = HttpStatusCode.BadRequest, Mensagem = ex.Message });
            }
            catch (Exception ex)
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                return Json(new { Status = HttpStatusCode.InternalServerError, Mensagem = ex.Message });
            }
        }
        private void ConfiguracaoCookiesGeral()
        {
            int maxRequestLength = 0;

            string extensoes = ConfigurationManager.AppSettings["extensoes"];

            HttpRuntimeSection section = ConfigurationManager.GetSection("system.web/httpRuntime") as HttpRuntimeSection;

            if (section != null)
            {
                maxRequestLength = section.MaxRequestLength;    
            }
                

            Response.Cookies.Add(new HttpCookie("ExtensoesArquivos", extensoes));
            Response.Cookies.Add(new HttpCookie("TamanhoArquivo", maxRequestLength.ToString()));

            var perfisSistema = _controleAcesso.ListarPerfisAplicacao();
            Response.Cookies.Add(new HttpCookie("PerfisSistema", HttpUtility.UrlEncode(JsonConvert.SerializeObject(perfisSistema), System.Text.Encoding.UTF8)));
        }

        private void CriarCookie(string login, UsuarioSistema usuario)
        {
            var usuarioJson = JsonConvert.SerializeObject(usuario);
            var usuarioCookie = new HttpCookie("UsuarioCAST", usuarioJson);
            Response.Cookies.Add(usuarioCookie);
            CreateAuthenticationTicket(usuario);
            FormsAuthentication.GetRedirectUrl(login, false);
        }

        public void CreateAuthenticationTicket(UsuarioSistema usuario)
        {
            try
            {
                TranspetroPrincipalSerializeModel serializeModel = new TranspetroPrincipalSerializeModel();
                serializeModel.Chave                 = usuario.Chave;
                serializeModel.Email                 = usuario.Email;
                serializeModel.Nome                  = usuario.Nome;
                serializeModel.ExecutaRelatorioChave = usuario.ExecutaRelatorioChave;

                if (usuario.Perfis != null && usuario.Perfis.Count() > 0)
                {
                    serializeModel.Perfis = usuario.Perfis.Select(p => new
                    TranspetroPrincipalPerfilSerializeModel()
                    {
                        IdCa          = p.id.ToString(),
                        Descricao     = p.nome,
                        Administrador = p.administrador

                    }).ToArray();
                }

                string userData = JsonConvert.SerializeObject(serializeModel);
                FormsAuthenticationTicket authTicket = new FormsAuthenticationTicket(
                  1, serializeModel.Chave, DateTime.Now, DateTime.Now.AddHours(8), true, userData);
                string encTicket = FormsAuthentication.Encrypt(authTicket);
                HttpCookie faCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encTicket);
                Response.Cookies.Add(faCookie);

            }
            catch (Exception ex)
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                throw ex;
            }
        }

        public RedirectResult Logout()
        {
            try
            {
                this.FinalizarAcesso();
                return Redirect("~/Login");
            }
            catch (Exception ex)
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                throw ex;
            }

        }

        private void FinalizarAcesso()
        {
            try
            {

                foreach (var item in Request.Cookies.AllKeys)
                {
                    Response.Cookies[item].Expires = DateTime.Now.AddYears(-1);
                }

                FormsAuthentication.SignOut();
                Session.Abandon();
                MemoryCache.Default.Dispose();
            }
            catch (Exception ex)
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                throw ex;
            }

        }
	}
}