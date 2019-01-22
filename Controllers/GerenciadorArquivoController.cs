using CAST.Business.Component;
using CAST.Common.Validation;
using Petrobras.Fcorp.DependencyInjection.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Web;
using System.Web.Mvc;

namespace CAST.Controllers
{
    public class GerenciadorArquivoController : Controller
    {
        private string diretorio = ConfigurationManager.AppSettings["diretorio"];

        public HttpRequestBase RequestUpload { get; set; }

        [HttpPost]
        public JsonResult Upload()
        {
            JsonResult result = new JsonResult();

            string fileName = string.Empty;

            try
            {
                HttpRequestBase requestUpload = RequestUpload != null ? RequestUpload : Request;

                HttpPostedFileBase file = requestUpload.Files[0] as HttpPostedFileBase;

                if (ArquivoEhValido(requestUpload))
                {
                    fileName = "Anexo_" + Guid.NewGuid() + Path.GetExtension(file.FileName);
                    var path = Path.Combine(diretorio, fileName);
                    file.SaveAs(path);
                   
                    result.Data = new { Status = HttpStatusCode.OK, ArquivoLogico = file.FileName, ArquivoFisico = fileName };

                    return result;
                }
                string Mensagem = AssertionConcern.Notifications.FirstOrDefault().Value.ToString();
                result.Data = new { Status = HttpStatusCode.BadRequest, Mensagem = Mensagem };

                return result;

            }
            catch (BusinessException ex)
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                
                result.Data = new { Status = HttpStatusCode.BadRequest, Mensagem = ex.Message };

                return result;
            }
            catch (Exception ex)
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                result.Data = new { Status = HttpStatusCode.InternalServerError, Mensagem = ex.Message };
                return result;

            }
        }



        [HttpPost]
        public JsonResult DeleteFile(string arquivo)
        {
            try
            {
                HttpStatusCode status = HttpStatusCode.OK;
                string mensagem = "Arquivo deletado com sucesso!";

                if (arquivo != null && arquivo != string.Empty)
                {
                    var path = Path.Combine(diretorio, arquivo);

                    if ((System.IO.File.Exists(path)))
                    {
                        System.IO.File.Delete(path);
                    }
                    else
                    {
                        status = HttpStatusCode.BadRequest;
                        mensagem = "Arquivo não encontrado!";
                    }
                }
                else
                {
                    status = HttpStatusCode.BadRequest;
                    mensagem = "Nome do arquivo não informado!";
                }


                return Json(new { Status = status, ArquivoLogico = arquivo, Mensagem = mensagem });


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

        public FileContentResult DownloadAnexo(string arquivo)
        {
            try
            {
                byte[] fileBytes = SalvarArquivo(arquivo);

                return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, arquivo);

            }
            catch (Exception)
            {

                throw;
            }

        }        


        private byte[] SalvarArquivo(string arqFisico)
        {
            var path = Path.Combine(diretorio, arqFisico);
            byte[] fileBytes = System.IO.File.ReadAllBytes(path);
            return fileBytes;
        }

        public JsonResult ValidaExtensaoArquivo(string arquivo)
        {

            try
            {
                if (ExtensaoValida(arquivo))
                {
                    return Json(new { Status = HttpStatusCode.OK }, JsonRequestBehavior.AllowGet);
                }

                string Mensagem = AssertionConcern.Notifications.FirstOrDefault().Value.ToString();

                return Json(new { Status = HttpStatusCode.BadRequest, Mensagem = Mensagem }, JsonRequestBehavior.AllowGet);

            }
            catch (BusinessException ex)
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                
                return Json(new { Status = HttpStatusCode.BadRequest, Mensagem = ex.Message },JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType).Error(ex);
                return Json(new { Status = HttpStatusCode.InternalServerError, Mensagem = ex.Message }, JsonRequestBehavior.AllowGet);

            }
        }

        private bool ArquivoEhValido(HttpRequestBase request)
        {
            string extensao = ConfigurationManager.AppSettings["extensoes"];
            string arquivo = request.Files.Count > 0 ? request.Files[0].FileName : "";

            return AssertionConcern.IsSatisfiedBy
            (
                AssertionConcern.AssertValueMoreThan(request.Files.Count, 1, "Upload com mais de um arquivo!"),
                AssertionConcern.AssertValueZero(request.Files.Count, "Nenhum arquivo foi enviado!"),
                AssertionConcern.AssertNotContains(Path.GetExtension(arquivo).Replace(".", ""), extensao, "Somente são permitida as extensões: " + extensao)
            );
        }

        private bool ExtensaoValida(string arquivo)
        {
            string extensao = ConfigurationManager.AppSettings["extensoes"];

            return AssertionConcern.IsSatisfiedBy
            (
                AssertionConcern.AssertNotContains(Path.GetExtension(arquivo).Replace(".", ""), extensao, "Somente são permitida as extensões: " + extensao)
            );
        }

        public bool DeleteFiles(string arquivo) 
        {
            if (arquivo != null && arquivo != string.Empty)
            {
                var path = Path.Combine(diretorio, arquivo);

                if ((System.IO.File.Exists(path)))
                {
                    System.IO.File.Delete(path);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

        }
    }
}