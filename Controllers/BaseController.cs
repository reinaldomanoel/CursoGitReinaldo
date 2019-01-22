using CAST.Business.Component.Security;

using CAST.Common.Validation; ABC

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace CAST.Controllers
{
    public class BaseController : Controller
    {
        public List<SelectListItem> CarregaCombo<T>(List<T> itens, bool adicionaLinha, object valor = null)
        {
            List<SelectListItem> items = new List<SelectListItem>();

            if (itens.Count() > 0)
            {
                items = new SelectList(itens,
                                       itens.ElementAt(0).GetType().GetProperties()[0].Name,
                                       itens.ElementAt(0).GetType().GetProperties()[1].Name,
                                       valor).ToList();
            }

            if (adicionaLinha)
            {

                items.Insert(0, (new SelectListItem { Text = "Todos", Value = "0" }));
            }
            
            return items;
        }

        public List<SelectListItem> CarregaCombo<T>(List<T> itens, int adicionaLinhaTexto, object valor = null)
        {
            List<SelectListItem> items = new List<SelectListItem>();

            if (itens.Count() > 0)
            {
                items = new SelectList(itens,
                                       itens.ElementAt(0).GetType().GetProperties()[0].Name,
                                       itens.ElementAt(0).GetType().GetProperties()[1].Name,
                                       valor).ToList();
            }

            if (adicionaLinhaTexto == 1)
            {

                items.Insert(0, (new SelectListItem { Text = "", Value = "0" }));
            }

            return items;
        }

        public UsuarioSistema DadosUsuario() 
        {
            return JsonConvert.DeserializeObject<UsuarioSistema>(Request.Cookies["UsuarioCAST"].Value);            
        }

        public JsonResult RetornoDados(object objeto)
        {
            return Json(new { Status = HttpStatusCode.OK, Dados = objeto }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult RetornoSucesso(string msg) 
        {
            return Json(new { Status = HttpStatusCode.OK, Mensagem = msg }, JsonRequestBehavior.AllowGet);       
        }

        public JsonResult RetornoMensagemErro(List<DomainNotification> mensagemErro)
        {
            var retorno = string.Join(",", mensagemErro.Select(p => p.Value));

            return Json(new { Status = HttpStatusCode.BadRequest, Mensagem = retorno }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult RetornoMensagemErro(string mensagemErro)
        {
            return Json(new { Status = HttpStatusCode.BadRequest, Mensagem = mensagemErro }, JsonRequestBehavior.AllowGet);
        }
	}
}
