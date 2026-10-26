import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { RecipeDetailDto, RecipeSummaryDto } from '../../models/recipe.models';
import { RecipeService } from './recipe.service';

describe('RecipeService', () => {
  let service: RecipeService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), RecipeService],
    });

    service = TestBed.inject(RecipeService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('fetches recipes list with query parameters via GET /api/recipes', async () => {
    const recipes: RecipeSummaryDto[] = [];
    const promise = firstValueFrom(
      service.getRecipes({ search: 'soup', status: 'Published', difficulty: 'Easy' }),
    );
    const req = httpMock.expectOne('/api/recipes?search=soup&status=Published&difficulty=Easy');
    expect(req.request.method).toBe('GET');
    req.flush(recipes);

    const result = await promise;
    expect(result).toEqual([]);
  });

  it('fetches recipe detail via GET /api/recipes/:id', async () => {
    const detail: RecipeDetailDto = {
      id: 'r-1',
      title: 'Lasagna',
      slug: 'lasagna',
      description: 'Yummy',
      status: 'Published',
      baseServings: 4,
      yieldDisplay: '1 pan',
      prepTimeMinutes: 20,
      cookTimeMinutes: 45,
      totalTimeMinutes: 65,
      difficulty: 'Medium',
      sourceUrl: null,
      sourceName: null,
      sourceAuthor: null,
      sourceBookTitle: null,
      sourceBookPage: null,
      coverImagePath: null,
      thumbnailImagePath: null,
      timesCooked: 0,
      lastCookedAt: null,
      averageRating: null,
      ingredients: [],
      steps: [],
      linkedSubRecipes: [],
      createdAt: '2026-01-01T00:00:00Z',
      createdBy: 'test-user',
      modifiedAt: '2026-01-01T00:00:00Z',
      modifiedBy: 'test-user',
    };

    const promise = firstValueFrom(service.getRecipeById('r-1'));
    const req = httpMock.expectOne('/api/recipes/r-1');
    expect(req.request.method).toBe('GET');
    req.flush(detail);

    const result = await promise;
    expect(result.title).toBe('Lasagna');
  });

  it('deletes recipe via DELETE /api/recipes/:id', async () => {
    const promise = firstValueFrom(service.deleteRecipe('r-1'));
    const req = httpMock.expectOne('/api/recipes/r-1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);

    await promise;
  });
});
