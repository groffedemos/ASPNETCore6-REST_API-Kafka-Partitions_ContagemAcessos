using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using APIContagem.Models;
using Confluent.Kafka;
using APIContagem.Extensions;

namespace APIContagem.Controllers;

[ApiController]
[Route("[controller]")]
public class ContadorController : ControllerBase
{
    private static readonly Contador _CONTADOR = new Contador();
    private readonly ILogger<ContadorController> _logger;
    private readonly IConfiguration _configuration;

    public ContadorController(ILogger<ContadorController> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet]
    public ResultadoContador Get()
    {
        int valorAtualContador;
        int partition;

        lock (_CONTADOR)
        {
            _CONTADOR.Incrementar();
            valorAtualContador = _CONTADOR.ValorAtual;
            partition = _CONTADOR.Partition;
        }

        var resultado = new ResultadoContador()
        {
            ValorAtual = valorAtualContador,
            Producer = _CONTADOR.Local,
            Kernel = _CONTADOR.Kernel,
            Framework = _CONTADOR.Framework,
            Mensagem = _configuration["MensagemVariavel"]
        };

        string topic = _configuration["ApacheKafka:Topic"];
        string jsonContagem = JsonSerializer.Serialize(resultado);

        using (var producer = KafkaExtensions.CreateProducer(_configuration))
        {
            var result = producer.ProduceAsync(
                new TopicPartition(topic, new Partition(partition)),
                new Message<Null, string>
                { Value = jsonContagem }).Result;

            _logger.LogInformation(
                $"Apache Kafka - Envio para o topico {topic} concluido | " +
                $"{jsonContagem} | Status: { result.Status.ToString()}");
        }

        return resultado;
    }
}