using CAST.Application.Interfaces;
using CAST.Application.ViewModel;
using CAST.Business.Component;
using CAST.Common.DTO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace CAST.Controllers
{
    public class SolicitacaoController : BaseController
    {
        private readonly ISolicitacaoAppService _solicitacaoAppService;


        public SolicitacaoController(ISolicitacaoAppService solicitacaoAppService)
        {
            _solicitacaoAppService = solicitacaoAppService;
        }
        //
        // GET: /Solicitacao/
        public ActionResult Remocao()
        {
            ViewBag.SistemaAlvo = CarregaCombo(_solicitacaoAppService.ListarSistemaAlvo(), 1);
            return View();
        }

        [HttpPost]
        public ActionResult DetalhesRemocao()
        {
            try
            {
                int modulo = 1;

                var chave = Request.Form["txtChave"];
                var codSistema = Convert.ToInt32(Request.Form["ddlSistemaAlvo"]);
                ViewBag.CodSolicitante = Request.Form["hdCodSolicitante"];

                var retorno = modulo > 0 ? _solicitacaoAppService.RetornarDadosUsusarioRemocao(chave, codSistema, modulo) : _solicitacaoAppService.RetornarDadosUsusarioRemocao(chave, codSistema);
                
                retorno.CodigoSistemaAlvo = Convert.ToInt32(Request.Form["ddlSistemaAlvo"]);

                return View(retorno);

            }
            catch (Exception ex)
            {
                ViewBag.SistemaAlvo = CarregaCombo(_solicitacaoAppService.ListarSistemaAlvo(), 1);
                ViewBag.MensgemErro = ex.Message;
                return View("Remocao");
            }
        }

        [HttpPost]
        public JsonResult ValidaAcesso(PedidoViewModel pedidoViewModel)
        {
            try
            {
                string json = string.Empty;

                string retorno = _solicitacaoAppService.ValidarAcesso(pedidoViewModel);

                return Json(new { Status = HttpStatusCode.OK, Retorno = retorno }, JsonRequestBehavior.AllowGet);
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
        public JsonResult CriarPedido()
        {
            try
            {
                PedidoViewModel pedidoViewModel = JsonConvert.DeserializeObject<PedidoViewModel>(Request.Form["pedido"]);

                if (Request.Files.Count != 0)
                {

                    dynamic jsonUpload = null;


                    HttpPostedFileBase file = Request.Files[0] as HttpPostedFileBase;

                    var gerenciadorArquivo = new GerenciadorArquivoController();

                    gerenciadorArquivo.RequestUpload = Request;

                    var resultado = gerenciadorArquivo.Upload() as JsonResult;

                    jsonUpload = resultado.Data;

                    if (jsonUpload.Status != HttpStatusCode.OK)
                    {
                        return Json(new { Status = HttpStatusCode.BadRequest, Codigo = 1, Mensagem = jsonUpload.Mensagem }, JsonRequestBehavior.AllowGet);
                    }

                    pedidoViewModel.ListPedidoObservacao.ToList().ForEach(o => o.AnexoPedido = jsonUpload.ArquivoFisico);
                }



                string json = string.Empty;

                _solicitacaoAppService.CriarPedido(pedidoViewModel);

                return Json(new { Status = HttpStatusCode.OK }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                return Json(new { Status = HttpStatusCode.InternalServerError, Codigo = 0, Mensagem = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult BuscaForcaTrabalho(string chave)
        {
            try
            {
                string json = string.Empty;

                JsonSerializerSettings js = new JsonSerializerSettings();
                js.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

                var dados = _solicitacaoAppService.BuscarForcaTrabalho(chave);

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

        public ActionResult Ajuda()
        {
            return View();
        }


        public ActionResult Acesso()
        {
            ViewBag.SistemaAlvo = CarregaCombo(_solicitacaoAppService.ListarSistemaAlvo(), 1);
            return View();
        }

        [HttpPost]
        public ActionResult DetalhesAcesso()
        {
            try
            {
                var chave = Request.Form["txtChave"];
                var codSistema = Convert.ToInt32(Request.Form["ddlSistemaAlvo"]);
                ViewBag.CodSolicitante = Request.Form["hdCodSolicitante"];

                int modulo = 1;

                //var retorno = _solicitacaoAppService.RetornarDadosUsusarioAcesso(chave, codSistema); 
                var retorno = modulo > 0 ? _solicitacaoAppService.RetornarDadosUsusarioAcesso(chave, codSistema, modulo) : _solicitacaoAppService.RetornarDadosUsusarioAcesso(chave, codSistema);
                
                retorno.CodigoSistemaAlvo = Convert.ToInt32(Request.Form["ddlSistemaAlvo"]);

                ViewBag.CodigoSistemaAlvo = retorno.CodigoSistemaAlvo;

                List<NivelAcessoViewModel> listaAcessosSistema;
                List<ItemComplementarPedidoViewModel> listaItens;

                _solicitacaoAppService.ListarAcessos(codSistema, out listaAcessosSistema, out listaItens);
                var listaAcessosUsuario = retorno.ListAcessosPedido.SelectMany(p => p.ListNiveisAcesso);

                ViewBag.AcessosSistema = listaAcessosUsuario != null ?
                                         listaAcessosSistema.Where(p => !listaAcessosUsuario.Any(u => u.CodigoTipoNivelAcesso == Convert.ToInt32(p.CodigoTipoNivelAcesso))).ToList() :
                                         listaAcessosSistema;

                var codigoItemComplementar = retorno.ListItensComplementarPedidos.Count > 0 ?
                                             (int?)retorno.ListItensComplementarPedidos.FirstOrDefault().CodigoTipoItemComplementar : null;
                if (listaItens.Count > 0)
                    ViewBag.CodigoItem = listaItens.FirstOrDefault().CodigoItemNoSistemaAlvo;

                if (listaItens.Count > 0)
                    ViewBag.NomeItem   = listaItens.FirstOrDefault().NomeItemNoSistemaAlvo;

                ViewBag.Valoracao = listaAcessosUsuario.FirstOrDefault().Valoracao;

                if (listaItens.Count > 0)
                    ViewBag.ItemComplementar = listaItens;
                        //CarregaCombo(listaItens.Select(p => new { Codigo = p.CodigoTipoItemComplementar, Descricao = p.NomeTipoItemComplementar }).ToList(), false, codigoItemComplementar);

                return View(retorno);

            }
            catch (Exception ex)
            {
                ViewBag.SistemaAlvo = CarregaCombo(_solicitacaoAppService.ListarSistemaAlvo(), 1);
                ViewBag.MensgemErro = ex.Message;
                return View("Acesso");
            }
        }

        public JsonResult VerificaPermissaoSistemaSolicitacao(int codigoSistema, int tipoSolicitacao)
        {
            try
            {
                string json = string.Empty;

                var dadosUsario = DadosUsuario();

                string retorno = _solicitacaoAppService.VerificaPermissaoSistemaSolicitacao(codigoSistema, tipoSolicitacao, dadosUsario.CodUsuario);

                return Json(new { Status = HttpStatusCode.OK, Retorno = retorno }, JsonRequestBehavior.AllowGet);
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

    }
}