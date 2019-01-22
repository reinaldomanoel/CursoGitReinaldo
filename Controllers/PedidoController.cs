using CAST.Application.Adapter;
using CAST.Application.Interfaces;
using CAST.Application.ViewModel;
using CAST.Business.Component;
using CAST.Business.Component.Security;
using CAST.Business.Entities;
using CAST.Common.DTO;
using CAST.Common.Enuns;
using CAST.Filters;
using CAST.Reports;
using Newtonsoft.Json;
using PagedList;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace CAST.Controllers
{
     [CustomAuthorize("Administrador de Sistema", "Gestor", "Administrador Geral", "Padrao")]
    public class PedidoController : BaseController
    {
        private readonly IPedidoAppService _pedidoAppService;

        public PedidoController(IPedidoAppService pedidoAppService)
        {
            _pedidoAppService = pedidoAppService;
        }

        public ActionResult Ajuda()
        {
            return View();
        }

        public ActionResult AjudaConsulta()
        {
            return View();
        }

        // Tela de Avaliar Pedido
        // GET: /Pedido/Index
        [CustomAuthorize("Administrador de Sistema","Gestor")]
        public ActionResult Index()
        {
            var pedidoFiltro = new PedidoFiltroDTO();
            UsuarioSistema usuarioLogado = JsonConvert.DeserializeObject<UsuarioSistema>(Request.Cookies["UsuarioCAST"].Value);
            pedidoFiltro.Pagina = 1;
            pedidoFiltro.CodigoUsuario = Convert.ToInt32(usuarioLogado.CodUsuario);
            var usuario = _pedidoAppService.RetornarDadosUsuario(pedidoFiltro.CodigoUsuario);
            pedidoFiltro.CodigoLotacaoUsuario = usuario.CodOrgao;
            ViewBag.PedidoFiltro = pedidoFiltro;

            Inicializar(pedidoFiltro.Avaliador, null, pedidoFiltro.SistemaAlvo, pedidoFiltro.CodigoLotacaoUsuario, pedidoFiltro.CodigoUsuario);

            return View();
        }

        // Tela Consultar Pedido
        // GET: /Pedido/Consulta
        [CustomAuthorize("Administrador de Sistema", "Gestor", "Administrador Geral", "Padrao")]
        public ActionResult Consulta()
        {
            var pedidoFiltro = new PedidoFiltroDTO();
            pedidoFiltro.Pagina = 1;
            ViewBag.PedidoFiltro = pedidoFiltro;

            InicializarConsulta(null, null, null, null);
            return View();
        }

        // Tela de Detalhes do Pedido
        public ActionResult Detalhes(int id, Operacao operacao, int? avaliador)
        {
            ForcaTrabalho forcaTrabalho;
            UsuarioSistema usuarioLogado = JsonConvert.DeserializeObject<UsuarioSistema>(Request.Cookies["UsuarioCAST"].Value);
            ViewBag.Avaliador  = avaliador;
            ViewBag.Operacao   = (int)operacao;
            ViewBag.CodUsuario = usuarioLogado.CodUsuario;

            PedidoViewModel pedido;

            if (operacao == Operacao.AvaliarPedido)
            {
                pedido = _pedidoAppService.BuscaPedidoAvaliacao(id, avaliador.Value);
            }
            else
            {
                pedido = _pedidoAppService.BuscaPedido(id);
            }

            //forcaTrabalho = _pedidoAppService.VerificaGestor(pedido.CodigoGestor, pedido.CodigoOrgaoSup);

            //pedido.NomeGestorUsuario  = forcaTrabalho != null ? forcaTrabalho.Nome  : "";
            //pedido.LoginGestorUsuario = forcaTrabalho != null ? forcaTrabalho.Login : "";

            return View("Detalhes", pedido);
        }

        private void InicializarConsulta(int? codTipoPedido, int? codSistemaAlvo, int? codOrgao, int? codSituacaoPedido)
        {
            UsuarioSistema usuarioLogado = JsonConvert.DeserializeObject<UsuarioSistema>(Request.Cookies["UsuarioCAST"].Value);
            ViewBag.CodUsuario           = usuarioLogado.CodUsuario;
            ViewBag.TipoPedido           = CarregaCombo(_pedidoAppService.ListarTipoPedido(), true, codTipoPedido);
            ViewBag.SistemaAlvo          = CarregaCombo(_pedidoAppService.ListarSistemaAlvo(), true, codSistemaAlvo);
            ViewBag.SituacaoPedido       = CarregaCombo(CarregaListaSituacaoPedido(), true, codSituacaoPedido);
            var usuario                  = _pedidoAppService.RetornarDadosUsuario(usuarioLogado.CodUsuario);
            ViewBag.CodOrgaoUsuario      = usuario.CodOrgao;

            ViewBag.Orgao = codOrgao == null
                            ? CarregaCombo(_pedidoAppService.ListarOrgao(), true, 0)
                            : CarregaCombo(_pedidoAppService.ListarOrgao(), true, codOrgao);
        }


        private List<SituacaoPedido> CarregaListaSituacaoPedido()
        {
            List<SituacaoPedido> ListaSituacaoPedido = new List<SituacaoPedido>();

            ListaSituacaoPedido.Add(new SituacaoPedido(){ Codigo = 1, Nome = "Em Andamento", ListPedidos = null } );
            ListaSituacaoPedido.Add(new SituacaoPedido() { Codigo = 2, Nome = "Encerrado", ListPedidos = null } );

            return ListaSituacaoPedido;
        }

        private void Inicializar(int? codAvaliador, int? codTipoPedido, int? codSistemaAlvo, int? codOrgao, int codUsuario)
        {

            var usuario = _pedidoAppService.RetornarDadosUsuario(codUsuario);
            ViewBag.CodOrgaoUsuario = usuario.CodOrgao;

            List<SelectListItem> papeis = new List<SelectListItem>();
            List<SelectListItem> sistemaAlvo = new List<SelectListItem>();

            if (usuario.Administrador)
                papeis.Add(new SelectListItem { Text = "Administrador", Value = "0" });

            if (usuario.Gestor)
                papeis.Add(new SelectListItem { Text = "Gestor", Value = "1" });

            if (codAvaliador == null && usuario.Administrador)
            {
                sistemaAlvo = CarregaCombo(_pedidoAppService.ListarSistemaAlvo(codUsuario), false, codSistemaAlvo);
            }
            else
            {
                if (codAvaliador == null && usuario.Gestor)
                {
                    sistemaAlvo = CarregaCombo(_pedidoAppService.ListarSistemaAlvo(), false, codSistemaAlvo);
                }
                else
                {
                    sistemaAlvo = codAvaliador == 1
                                        ? CarregaCombo(_pedidoAppService.ListarSistemaAlvo(), true, codSistemaAlvo)
                                        : CarregaCombo(_pedidoAppService.ListarSistemaAlvo(codUsuario), false, codSistemaAlvo);
                }
            }


            ViewBag.TipoPedido = usuario.Administrador == true ? CarregaCombo(_pedidoAppService.ListarTipoPedidoAvaliador(0), true, codTipoPedido)
                                                               : CarregaCombo(_pedidoAppService.ListarTipoPedidoAvaliador(1), true, codTipoPedido);

            ViewBag.CodAvaliador = usuario.Administrador ? 0 : 1;

            ViewBag.SistemaAlvo = sistemaAlvo;

            ViewBag.Avaliador = new SelectList(papeis, "Value", "Text", codAvaliador);

            List<SelectListItem> comboOrgao;

            codOrgao = codOrgao == null ? usuario.CodOrgao : codOrgao;

            if (codAvaliador == null && usuario.Administrador)
            {
                comboOrgao = CarregaCombo(_pedidoAppService.ListarOrgao(), true, codOrgao);
            }
            else
            {
                if (codAvaliador == 0)
                {
                    comboOrgao = CarregaCombo(_pedidoAppService.ListarOrgao(), true, codOrgao);
                }
                else
                {
                    comboOrgao = CarregaCombo(_pedidoAppService.ListarOrgao(usuario.Codigo), false, codOrgao);
                }
            }

            ViewBag.Orgao = comboOrgao;
        }

        //Pesquisar da tela Avaliar Pedidos
        [HttpPost]
        public ActionResult Pesquisar()
        {
            var pedidoFiltro = PedidoFiltroAdapter.ToPedidoFiltro(Request.Form);

            //var lista = _pedidoAppService.ListarAvaliarPedido(PedidoFiltroAdapter.ToFiltro(pedidoFiltro), PedidoFiltroAdapter.ToOrdeBy(pedidoFiltro));
            var lista = _pedidoAppService.Listar(PedidoFiltroAdapter.ToFiltroSql(pedidoFiltro), PedidoFiltroAdapter.ToOrdeBySql());

            if (lista.Count <= 5)
            {
                pedidoFiltro.Pagina = 1;
            }

            PagedList<PedidoViewModel> listaPaginada = new PagedList<PedidoViewModel>(lista, pedidoFiltro.Pagina, 5);

            ViewBag.PedidoFiltro = pedidoFiltro;

            Inicializar(pedidoFiltro.Avaliador, pedidoFiltro.CodigoTipoPedido, pedidoFiltro.SistemaAlvo, pedidoFiltro.CodigoLotacao, pedidoFiltro.CodigoUsuario);

            return View("Index", listaPaginada);
        }

        //Pesquisar da tela Consultar Pedidos
        [HttpPost]
        public ActionResult Consultar()
        {
            var pedidoFiltro = PedidoFiltroAdapter.ToPedidoFiltro(Request.Form);

            var lista = _pedidoAppService.Listar(PedidoFiltroAdapter.ToFiltroSql(pedidoFiltro), PedidoFiltroAdapter.ToOrdeBySql());

            if (lista.Count <= 5)
            {
                pedidoFiltro.Pagina = 1;
            }

            PagedList<PedidoViewModel> listaPaginada = new PagedList<PedidoViewModel>(lista, pedidoFiltro.Pagina, 5);

            ViewBag.PedidoFiltro = pedidoFiltro;

            InicializarConsulta(pedidoFiltro.CodigoTipoPedido, pedidoFiltro.SistemaAlvo, pedidoFiltro.CodigoLotacao, pedidoFiltro.Status);

            return View("Consulta", listaPaginada);
        }

        public JsonResult BuscaForcaTrabalho(string chave)
        {
            try
            {
                string json = string.Empty;

                JsonSerializerSettings js = new JsonSerializerSettings();
                js.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

                var dados = _pedidoAppService.BuscarForcaTrabalho(chave);

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

        public JsonResult BuscaDadosAvaliador(int codigotipoAvaliador, int codUsuario)
        {
            string jsonSistemaAlvo = string.Empty;
            string jsonOrgao       = string.Empty;
            string jsonTipoPedido  = string.Empty;

            JsonSerializerSettings js = new JsonSerializerSettings();
            js.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            try
            {
                var dadosSistemaAlvo = codigotipoAvaliador == 1
                                                    ? _pedidoAppService.ListarSistemaAlvo()
                                                    : _pedidoAppService.ListarSistemaAlvo(codUsuario);

                jsonSistemaAlvo = JsonConvert.SerializeObject(dadosSistemaAlvo, Formatting.None, js);

                var dadosOrgao = codigotipoAvaliador == 0
                                                    ? _pedidoAppService.ListarOrgao()
                                                    : _pedidoAppService.ListarOrgao(codUsuario);

                jsonOrgao = JsonConvert.SerializeObject(dadosOrgao, Formatting.None, js);

                var dadosTipoPedido = codigotipoAvaliador == 0 ? _pedidoAppService.ListarTipoPedidoAvaliador(0)
                                                               : _pedidoAppService.ListarTipoPedidoAvaliador(1);

                jsonTipoPedido = JsonConvert.SerializeObject(dadosTipoPedido, Formatting.None, js);


                return Json(new
                {
                    Status           = HttpStatusCode.OK,
                    DadosSistemaAlvo = jsonSistemaAlvo,
                    DadosOrgao       = jsonOrgao,
                    DadosTipoPedido  = jsonTipoPedido
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Status = HttpStatusCode.InternalServerError, Codigo = 0, Mensagem = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult VerificaSeGestor(int codigoPedido, int codigoUsuarioSessao)
        {
            try
            {
                var resultado = _pedidoAppService.ValidaUsuarioAvaliadorDoPedido(codigoPedido, codigoUsuarioSessao);
                return Json(new { Status = HttpStatusCode.OK, Dados = resultado }, JsonRequestBehavior.AllowGet);
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

        public JsonResult VerificaPedidoAptoAvaliacao(int codigoPedido)
        {
            try
            {
                var pedido = _pedidoAppService.BuscaPedido(codigoPedido);

                bool resultado = pedido.DataExpiracao.Date >= DateTime.Now.Date;

                return Json(new { Status = HttpStatusCode.OK, Dados = resultado }, JsonRequestBehavior.AllowGet);
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
        public JsonResult Finalizar()
        {
            try
            {

                var pedido = _pedidoAppService.BuscaPedido(Convert.ToInt32(Request.Form["Codigo"]));

                if (pedido.CodigoSituacaoPedido == (int) TipoSituacaoPedido.Encerrado)
                    return Json(new { Status = HttpStatusCode.BadRequest, Codigo = 0, Mensagem = "Pedido já avaliado!" }, JsonRequestBehavior.AllowGet);

                NameValueCollection valores = new NameValueCollection();

                if (Request.Files.Count != 0) {

                    HttpPostedFileBase file = Request.Files[0] as HttpPostedFileBase;

                    var gerenciadorArquivo = new GerenciadorArquivoController();

                    gerenciadorArquivo.RequestUpload = Request;

                    var resultado = gerenciadorArquivo.Upload() as JsonResult;

                    dynamic jsonUpload = resultado.Data;

                    if (jsonUpload.Status != HttpStatusCode.OK)
                    {
                        return Json(new { Status = HttpStatusCode.BadRequest, Codigo = 1, Mensagem = jsonUpload.Mensagem }, JsonRequestBehavior.AllowGet);
                    }

                    valores.Add("Arquivo", jsonUpload.ArquivoFisico);
                }
                
                foreach (var item in Request.Form.Keys)
                {
                    valores.Add(item.ToString(), Request[item.ToString()]);
                }

                _pedidoAppService.FinalizarAvaliacao(AvaliacaoPedidoAdapter.ToAvaliaPedido(valores));

                return Json(new { Status = HttpStatusCode.OK, Codigo = 1, Mensagem = "OK" }, JsonRequestBehavior.AllowGet);

            }
            catch (BusinessException ex)
            {
                return Json(new { Status = HttpStatusCode.BadRequest, Codigo = 0, Mensagem = ex.Message }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                return Json(new { c = HttpStatusCode.InternalServerError, Codigo = 0, Mensagem = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult ExportaArquivo(int codigoPedido) 
        {
            var pedido = _pedidoAppService.BuscaPedido(codigoPedido);

            List<PedidoViewModel> pedidos = new List<PedidoViewModel>();

            pedidos.Add(pedido);

            CultureInfo cult = new CultureInfo("pt-BR");
            string nomeArquivo = string.Format("ConsultaPedido-{0}.pdf", DateTime.Now.ToString("yyyyMMdd_HHmmss", cult));

            var relUtil = new GerarRelatorio();

            relUtil.DadosPedido = pedidos;
        
            Session[nomeArquivo] = relUtil.ExportarPdf(nomeArquivo);

            return Json(new { Status = HttpStatusCode.OK, NomeArquivo = nomeArquivo }, JsonRequestBehavior.AllowGet);

        }

        public ActionResult DownloadPdf(string nomeArquivo)
        {

            var ms = Session[nomeArquivo] as byte[];

            if (ms == null)
                return new EmptyResult();
            
            Session[nomeArquivo] = null;

            return File(ms, "application/pdf", nomeArquivo);
        }
    }
}