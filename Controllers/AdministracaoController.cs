using CAST.Application.Interfaces;
using CAST.Application.ViewModel;
using CAST.Business.Component;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace CAST.Controllers
{
    //[Authorize]
    public class AdministracaoController : BaseController
    {
        private readonly IAdministradorSistemaAlvoAppService _administradorAppService;
        public AdministracaoController(IAdministradorSistemaAlvoAppService administradorAppService)
        {
            _administradorAppService = administradorAppService;
        }


        //
        // GET: /Administracao
        public ActionResult Index()
        {
            int codSistema = 0;
            return View(Inicializar(codSistema));

        }

        public ActionResult Ajuda()
        {
            return View();
        }

        private List<AdministradorSistemaAlvoViewModel> Inicializar(int codSistema)
        {
            ViewBag.Sistema = CarregaCombo(_administradorAppService.ListarSistemaAlvo(), true, codSistema);
            List<AdministradorSistemaAlvoViewModel> lista = new List<AdministradorSistemaAlvoViewModel>();
            if (codSistema == 0)
            {
                lista = _administradorAppService.ListarTodos();
            }
            else
            {
                lista = _administradorAppService.ListarTodos(codSistema);
            }

            return lista;
        }


        public JsonResult IncluirAdministradorSistemaAlvo(int codSistema, string chave)
        {
            //int codSistema = Convert.ToInt32(Request["ddlsistema"]);
            //string chave   = Request["chave"];

            try
            {
                string json = string.Empty;

                JsonSerializerSettings js = new JsonSerializerSettings();
                js.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

                var dados = _administradorAppService.IncluirAdministradorSistemaAlvo(codSistema, chave);

                if (dados != null)
                {
                    json = JsonConvert.SerializeObject(dados, Formatting.None, js);
                }

                return Json(new { Status = Convert.ToInt32(HttpStatusCode.OK), Dados = json }, JsonRequestBehavior.AllowGet);
            }
            catch (BusinessException ex)
            {
                return Json(new { Status = Convert.ToInt32(HttpStatusCode.BadRequest), Codigo = 0, Mensagem = ex.Message }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                return Json(new { Status = Convert.ToInt32(HttpStatusCode.InternalServerError), Codigo = 0, Mensagem = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        public ActionResult ExcluirAdministradorSistemaAlvo(int codSistema, string chave)
        {
            try
            {
                _administradorAppService.ExcluirAdministradorSistemaAlvo(codSistema, chave);
                TempData["MensagemExclusao"] = "Administrador Excluido com sucesso.";
                return View("Index", Inicializar(0));
            }
            catch (Exception ex)
            {
                TempData["MensagemExclusao"] = ex.Message;
                return View("Index", Inicializar(0));
            }


        }
        public JsonResult BuscaForcaTrabalho(string chave)
        {
            try
            {
                string json = string.Empty;

                JsonSerializerSettings js = new JsonSerializerSettings();
                js.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

                var dados = _administradorAppService.BuscarForcaTrabalho(chave);

                if (dados != null)
                {
                    json = JsonConvert.SerializeObject(dados, Formatting.None, js);
                }

                return Json(new { Status = HttpStatusCode.OK, Dados = json }, JsonRequestBehavior.AllowGet);
            }
            catch (BusinessException ex)
            {
                return Json(new { Status = HttpStatusCode.BadRequest, Codigo = 0, Mensagem = ex.Message }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                return Json(new { Status = HttpStatusCode.InternalServerError, Codigo = 0, Mensagem = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public ActionResult FiltrarSistemaAlvo(int codSistema = 0)
        {
            //int? sistema = Request["ddlsistema"] != "0" ? (int?)Convert.ToInt32(Request["ddlsistema"]) : null;
            int sistema = Convert.ToInt32(Request["ddlsistema"]);
            return View("Index", Inicializar(sistema));
        }

    }
}