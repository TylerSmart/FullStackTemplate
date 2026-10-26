import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink } from '@angular/router';
import {
  RecipeDifficulty,
  RecipeQueryParameters,
  RecipeStatus,
  RecipeSummaryDto,
} from '../../core/models/recipe.models';
import { RecipeService } from '../../core/services/recipe/recipe.service';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  imports: [
    FormsModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatChipsModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
  ],
  selector: 'app-recipe-list',
  templateUrl: './recipe-list.component.html',
  styleUrl: './recipe-list.component.scss',
})
export class RecipeListComponent implements OnInit {
  private readonly recipeService = inject(RecipeService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly recipes = signal<RecipeSummaryDto[]>([]);
  protected readonly searchQuery = signal<string>('');
  protected readonly selectedStatus = signal<RecipeStatus | 'All'>('Published');
  protected readonly selectedDifficulty = signal<RecipeDifficulty | 'All'>('All');
  protected readonly maxTimeMinutes = signal<number | null>(null);
  protected readonly isLoading = signal<boolean>(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly filteredRecipes = computed(() => {
    const query = this.searchQuery().toLowerCase().trim();
    const status = this.selectedStatus();
    const difficulty = this.selectedDifficulty();
    const maxTime = this.maxTimeMinutes();

    return this.recipes().filter((r) => {
      if (status !== 'All' && r.status !== status) {
        return false;
      }
      if (difficulty !== 'All' && r.difficulty !== difficulty) {
        return false;
      }
      if (maxTime !== null && (r.totalTimeMinutes === null || r.totalTimeMinutes > maxTime)) {
        return false;
      }
      if (!query) {
        return true;
      }
      const matchTitle = r.title.toLowerCase().includes(query);
      const matchDesc = (r.description || '').toLowerCase().includes(query);
      const matchSource = (r.sourceName || '').toLowerCase().includes(query);
      return matchTitle || matchDesc || matchSource;
    });
  });

  ngOnInit(): void {
    this.loadRecipes();
  }

  loadRecipes(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    // Request non-archived or all based on query
    const params: RecipeQueryParameters = {};
    if (this.selectedStatus() !== 'All') {
      params.status = this.selectedStatus() as RecipeStatus;
    }

    this.recipeService.getRecipes(params).subscribe({
      next: (list) => {
        this.recipes.set(list);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(
          err?.error?.detail || err?.error?.title || 'Failed to load cookbook recipes.',
        );
      },
    });
  }

  onStatusChange(status: RecipeStatus | 'All'): void {
    this.selectedStatus.set(status);
    this.loadRecipes();
  }

  deleteRecipe(recipe: RecipeSummaryDto, event: Event): void {
    event.stopPropagation();
    event.preventDefault();

    const confirmRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Delete Recipe',
        message: `Are you sure you want to delete "${recipe.title}"?`,
        confirmText: 'Delete',
        isDestructive: true,
      },
    });

    confirmRef.afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) {
        return;
      }

      this.recipeService.deleteRecipe(recipe.id).subscribe({
        next: () => {
          this.recipes.update((list) => list.filter((r) => r.id !== recipe.id));
          this.snackBar.open(`Deleted "${recipe.title}"`, 'Close', { duration: 3000 });
        },
        error: (err) => {
          this.snackBar.open(
            err?.error?.detail || err?.error?.title || 'Cannot delete recipe.',
            'Close',
            { duration: 5000 },
          );
        },
      });
    });
  }
}
