using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.VectorData;

namespace SemanticKernelOllamaSearch
{

    public class VectorDbContext : DbContext
    {
        public VectorDbContext(DbContextOptions<VectorDbContext> options) : base(options) { }

        public DbSet<DocumentChunk> Chunks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("vector");

            modelBuilder.Entity<DocumentChunk>()
                .HasIndex(c => c.Vector)
                .HasMethod("hnsw")
                .HasOperators("vector_cosine_ops");
        }
    }

    public class DocumentChunk
    {
        [VectorStoreKey]
        public int Id { get; set; }
        [VectorStoreData]
        public string Content { get; set; } = string.Empty;
        [VectorStoreData]
        public string SourceDocument { get; set; } = string.Empty;

        // Burada 384 boyutlu vektör kullanıyoruz çünkü 

        [VectorStoreVector(384, DistanceFunction = DistanceFunction.CosineSimilarity)]
        public ReadOnlyMemory<float> Vector { get; set; }
    }
}
