using CAST.Application.Adapter;
using CAST.Application.Interfaces;
using CAST.Application.ViewModel;
using CAST.Business.Component;
using CAST.Common.Helpers;
using CAST.Filters;
using CAST.Reports;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PagedList;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace CAST.Controllers
{
    [CustomAuthorize("Administrador de Sistema")]
    public class RevisaoController : BaseController
    {
        private readonly IRevisaoAppService _revisaoAppService;
        private readonly IPedidoAppService  _pedidoAppService;

        public RevisaoController(IRevisaoAppService revisaoAppService,
                                 IPedidoAppService  pedidoAppService)
        {
            _revisaoAppService = revisaoAppService;
            _pedidoAppService  = pedidoAppService; 
        }
        
        // GET: /Revisao/        
        public ActionResult Index()
        {
            ViewBag.Pagina = 1;
            Inicializar(null, null);
            return View();
        }

        public ActionResult Ajuda()
        {
            return View();
        }

        private void Inicializar(int? codSistema, int? codStatus)
        {
            ViewBag.SistemaAlvo     = CarregaCombo(_revisaoAppService.ListarSistemasAlvo(), true, codSistema);
            ViewBag.SistemaAlvoNovo = CarregaCombo(_revisaoAppService.ListarSistemasAlvo(), 1, codSistema);
            ViewBag.Status          = CarregaCombo(_revisaoAppService.ListarSituacaoRevisao(), true, codStatus);
        }

        
        [HttpPost]
        public ActionResult Pesquisar() 
        {
            int? codSistema      = Request["ddlSistemaAlvo"] != "0" ? (int?)Convert.ToInt32(Request["ddlSistemaAlvo"])  : null;
            int? codStatus       = Request["ddlStatus"]  != "0"  ? (int?)Convert.ToInt32(Request["ddlStatus"])          : null;
            DateTime? dataInicio = Request["dataInicio"] != null ? (DateTime?)Convert.ToDateTime(Request["dataInicio"]) : null;
            DateTime? dataFim    = Request["dataFim"]    != null ? (DateTime?)Convert.ToDateTime(Request["dataFim"])    : null;
            ViewBag.Pagina       = Convert.ToInt32(Request["hdPagina"]);

            var lista = _revisaoAppService.Listar(1, 10, codSistema, codStatus, dataInicio, dataFim);

            if (lista.Count <= 5)
            {
                ViewBag.Pagina = 1;
            }

            PagedList<RevisaoViewModel> listaPaginada = new PagedList<RevisaoViewModel>(lista, ViewBag.Pagina, 5);


            ViewBag.DataInicial = dataInicio != null ? dataInicio : null;
            ViewBag.DataFInal   = dataFim != null ? dataFim : null;

            Inicializar(((int?)codSistema), codStatus);

            return View("Index", listaPaginada);    
        }

        public JsonResult RetornarDiasPeriodoRevisao(int codSistemaAlvo)
        {
            try
            {
                var qntDias = _revisaoAppService.RetornarDiasPeriodoRevisao(codSistemaAlvo);
                return Json(new { Status = HttpStatusCode.OK, Codigo = 1, Dados = qntDias }, JsonRequestBehavior.AllowGet);
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

        [ModelStateValidationActionFilter]
        [HttpPost]
        public JsonResult CadastrarRevisao(RevisaoViewModel revisaoViewModel)
        {
            try
            {
                _revisaoAppService.Cadastrar(RevisaoAdapter.ToRevisao(revisaoViewModel));
                return RetornoSucesso("Revisão cadastrada com sucesso!");
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

        [ModelStateValidationActionFilter]
        [HttpPut]
        public JsonResult AlterarRevisao(RevisaoViewModel revisaoViewModel)
        {
            try
            {
                _revisaoAppService.Alterar(RevisaoAdapter.ToRevisao(revisaoViewModel));
                return RetornoSucesso("Revisão alterada com sucesso!");
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
                
        [HttpDelete]
        public JsonResult CancelarRevisao(int cdRevisao)
        {
            try
            {
                _revisaoAppService.Cancelar(cdRevisao);
                return RetornoSucesso("Revisão cancelada com sucesso!");
            }           
            catch (Exception ex)
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                return Json(new { Status = HttpStatusCode.InternalServerError, Codigo = 0, Mensagem = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        

        public JsonResult ExportaArquivoPdf(int cdRevisao)
        {
            try
            {
                var revisao = _revisaoAppService.BuscarRevisao(cdRevisao);

                List<RevisaoViewModel> dadosRevisao = new List<RevisaoViewModel>();

                dadosRevisao.Add(revisao);

                CultureInfo cult = new CultureInfo("pt-BR");
                string nomeArquivo = string.Format("ConsultaRevisao-{0}.pdf", DateTime.Now.ToString("yyyyMMdd_HHmmss", cult));

                var relUtil = new GerarRelatorioRevisao();

                var dadosPedido = _pedidoAppService.ListaPedidosRevisao(cdRevisao);

                relUtil.DadosRevisao = dadosRevisao;
                relUtil.DadosPedido  = dadosPedido;

                Session[nomeArquivo] = relUtil.ExportarPdf(nomeArquivo);

                return Json(new { Status = HttpStatusCode.OK, NomeArquivo = nomeArquivo }, JsonRequestBehavior.AllowGet);


            }
            catch (Exception ex)
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                return Json(new { Status = HttpStatusCode.InternalServerError, Erro = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        public ActionResult DownloadPdf(string nomeArquivo)
        {

            var ms = Session[nomeArquivo] as byte[];

            if (ms == null)
            {
                return new EmptyResult();
            }
                

            Session[nomeArquivo] = null;

            return File(ms, "application/pdf", nomeArquivo);
        }

        public JsonResult BuscarRevisao(int cdRevisao)
        {
            try
            {
                string json = string.Empty;

                JsonSerializerSettings js = new JsonSerializerSettings();
                js.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

                var dados = _revisaoAppService.BuscarRevisao(cdRevisao);

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

        public JsonResult RetornaSituacaoSistemaAlvo(int codSistemaAlvo)
        {
            try
            {
                var statusSistemaAlvo = _revisaoAppService.BuscaStatusSistemaAlvo(codSistemaAlvo);
                return Json(new { Status = HttpStatusCode.OK, Codigo = 1, Dados = statusSistemaAlvo }, JsonRequestBehavior.AllowGet);
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

        public JsonResult RetornaStatusTrocaLotacaoSistemaAlvo(int codSistemaAlvo)
        {
            try
            {
                var statusSistemaAlvo = _revisaoAppService.BuscaStatusTrocaLotacaoSistemaAlvo(codSistemaAlvo);
                return Json(new { Status = HttpStatusCode.OK, Codigo = 1, Dados = statusSistemaAlvo }, JsonRequestBehavior.AllowGet);
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

        [HttpGet]
        public JsonResult RetornaFuncoesFuncionarios()
        {
            try
            {
                var funcoes = _revisaoAppService.ListarFuncoes();
                return Json(new { Status = HttpStatusCode.OK, Dados = funcoes }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                return Json(new { Status = HttpStatusCode.InternalServerError, Codigo = 0, Mensagem = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult RetornaAcessosSistema(int id) 
        {
            try
            {
                var acessos = _revisaoAppService.ListarAcessos(id);
                return Json(new { Status = HttpStatusCode.OK, Dados = acessos }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                return Json(new { Status = HttpStatusCode.InternalServerError, Codigo = 0, Mensagem = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GeraArquivo(int codRevisao)
        {
            try
            {
                /** REVISÃO **/
                var revisao = _revisaoAppService.ListarRevisaoGeraArquivoNew(codRevisao);

                string[] cabecalhoRevisao = new[] { "Sistema", "Data Início", "Data Fim", "Situação", "Responsável", "Histórico da Revisão", "Dispensa de Revisão" };

                PlanilhaHelper excel = new PlanilhaHelper();

                excel.AdicionarPlanilha("Dados da Revisão");

                excel.GerarPlanilhaRevisao(cabecalhoRevisao, revisao);
                    
                /** PEDIDO **/

                var pedidos = _pedidoAppService.ListarPedidosGeraArquivo(codRevisao);

                string[] cabecalhoPedido = new[] {
                                                    "Número do Pedido", "Tipo do Pedido", "Usuário do Pedido", "Data da criação",
                                                    "Data da expiração", "Situação do Pedido", "Usuário Solicitante", "E-mail", "Ramal",
                                                    "Gestor", "Avaliador", "Histórico do Pedido[Data Orcorrência|Responsável|Descrição]", "Observações da Avaliação",
                                                    "Codigo do Acesso", "Situação do Acesso", "Nível",  "Conteúdo do Nível", "Histórico do Acesso[Data Orcorrência|Responsável|Descrição]"
                                                 };

                excel.AdicionarPlanilha("Dados dos Pedidos");

                excel.GerarPlanilhaPedido(cabecalhoPedido, pedidos);
                
                string handle = Guid.NewGuid().ToString();

                TempData[handle] = excel.RetornarPlanilha();

                CultureInfo cult = new CultureInfo("pt-BR");
                string nomeArquivo = "HistoricoDocumento_" + DateTime.Now.ToString("dd/MM/yyyy_HHmmss", cult) + ".xls";

                return Json(new { FileGuid = handle, FileName = nomeArquivo }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                throw;
            }
        }

        public virtual ActionResult DownloadArquivo(string fileGuid, string fileName)
        {
            if (TempData[fileGuid] != null)
            {
                byte[] data = TempData[fileGuid] as byte[];
                return File(data, "application/vnd.ms-excel", fileName);
            }
            else
            {
                return new EmptyResult();
            }
        }

        [HttpGet]
        public JsonResult RetornaDispensasPorRevisao(int id)
        {
            try
            {
                var dispensas = _revisaoAppService.ListarDispensasPorRevisao(id);
                return Json(new { Status = HttpStatusCode.OK, Dados = dispensas }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                return Json(new { Status = HttpStatusCode.InternalServerError, Codigo = 0, Mensagem = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}